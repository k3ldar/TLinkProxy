using System.Buffers;
using System.Text;

#pragma warning disable CC0009, CC0010

namespace TLinkProxy.Proxy.Streaming
{
    /// <summary>
    /// Relays a server-sent-events stream to the client as it arrives instead of buffering the whole
    /// response. Events are forwarded immediately until one carries tool calls; from that point the
    /// remainder is held so the tool-call policy can inspect (and, if required, rewrite) it before
    /// it is sent.
    /// </summary>
    internal static class SseStreamRelay
    {
        private const int ReadBufferSize = 8192;
        private const byte LineFeed = (byte)'\n';
        private const byte CarriageReturn = (byte)'\r';

        public static async Task RelayAsync(
            Stream source,
            Stream destination,
            Func<string, Task<string>> inspectAsync,
            Func<ReadOnlyMemory<byte>, Task> onChunkAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            using PooledByteBuffer pending = new();
            PooledByteBuffer held = null;

            try
            {
                while (true)
                {
                    int read = await source.ReadAsync(readBuffer.AsMemory(0, ReadBufferSize), cancellationToken).ConfigureAwait(false);

                    if (read == 0)
                    {
                        break;
                    }

                    ReadOnlyMemory<byte> chunk = readBuffer.AsMemory(0, read);

                    if (onChunkAsync != null)
                    {
                        await onChunkAsync(chunk).ConfigureAwait(false);
                    }

                    if (held != null)
                    {
                        held.Append(chunk.Span);
                        continue;
                    }

                    pending.Append(chunk.Span);
                    held = await ForwardCompleteEventsAsync(pending, destination, cancellationToken).ConfigureAwait(false);
                }

                if (held != null)
                {
                    await WriteInspectedAsync(held, destination, inspectAsync, cancellationToken).ConfigureAwait(false);
                }
                else if (pending.Length > 0)
                {
                    // Trailing data that never reached an event boundary is still passed through.
                    await destination.WriteAsync(pending.Memory, cancellationToken).ConfigureAwait(false);
                }

                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer);
                held?.Dispose();
            }
        }

        /// <summary>
        /// Returns the length of the first complete event (including its terminating blank line) or -1
        /// when the data does not yet contain a complete event.
        /// </summary>
        internal static int FindEventLength(ReadOnlySpan<byte> data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != LineFeed)
                {
                    continue;
                }

                int next = i + 1;

                if (next < data.Length && data[next] == CarriageReturn)
                {
                    next++;
                }

                if (next < data.Length && data[next] == LineFeed)
                {
                    return next + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// True when the event contains a non-empty <c>tool_calls</c> array; <c>"tool_calls":null</c>
        /// and <c>"tool_calls":[]</c> are ignored so they do not force buffering.
        /// </summary>
        internal static bool ContainsToolCalls(ReadOnlySpan<byte> sseEvent)
        {
            ReadOnlySpan<byte> key = "\"tool_calls\""u8;
            int searchFrom = 0;

            while (searchFrom < sseEvent.Length)
            {
                int index = sseEvent[searchFrom..].IndexOf(key);

                if (index < 0)
                {
                    return false;
                }

                int position = SkipWhitespace(sseEvent, searchFrom + index + key.Length);

                if (position < sseEvent.Length && sseEvent[position] == (byte)':')
                {
                    position = SkipWhitespace(sseEvent, position + 1);

                    if (position < sseEvent.Length && sseEvent[position] == (byte)'[')
                    {
                        int inner = SkipWhitespace(sseEvent, position + 1);

                        if (inner >= sseEvent.Length || sseEvent[inner] != (byte)']')
                        {
                            return true;
                        }
                    }
                }

                searchFrom += index + key.Length;
            }

            return false;
        }

        private static int SkipWhitespace(ReadOnlySpan<byte> data, int position)
        {
            while (position < data.Length && (data[position] == (byte)' ' || data[position] == (byte)'\t' || data[position] == CarriageReturn || data[position] == LineFeed))
            {
                position++;
            }

            return position;
        }

        private static int FindForwardableLength(ReadOnlySpan<byte> data, out bool toolCallDetected)
        {
            int offset = 0;
            toolCallDetected = false;

            while (true)
            {
                int length = FindEventLength(data[offset..]);

                if (length < 0)
                {
                    break;
                }

                if (ContainsToolCalls(data.Slice(offset, length)))
                {
                    toolCallDetected = true;
                    break;
                }

                offset += length;
            }

            return offset;
        }

        /// <summary>
        /// Writes every complete event that carries no tool calls. When a tool-call event is found, the
        /// remaining data is moved into a new buffer that is returned so the caller can hold it.
        /// </summary>
        private static async Task<PooledByteBuffer> ForwardCompleteEventsAsync(PooledByteBuffer pending, Stream destination, CancellationToken cancellationToken)
        {
            int forwardable = FindForwardableLength(pending.Span, out bool toolCallDetected);

            if (forwardable > 0)
            {
                await destination.WriteAsync(pending.Memory[..forwardable], cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                pending.Consume(forwardable);
            }

            if (!toolCallDetected)
            {
                return null;
            }

            PooledByteBuffer held = new(pending.Length);
            held.Append(pending.Span);
            pending.Consume(pending.Length);

            return held;
        }

        private static async Task WriteInspectedAsync(PooledByteBuffer held, Stream destination, Func<string, Task<string>> inspectAsync, CancellationToken cancellationToken)
        {
            if (inspectAsync == null)
            {
                await destination.WriteAsync(held.Memory, cancellationToken).ConfigureAwait(false);
                return;
            }

            string text = Encoding.UTF8.GetString(held.Span);
            string inspected = await inspectAsync(text).ConfigureAwait(false);

            if (inspected == null || String.Equals(inspected, text, StringComparison.Ordinal))
            {
                // Unchanged: write the original bytes rather than re-encoding the text.
                await destination.WriteAsync(held.Memory, cancellationToken).ConfigureAwait(false);
                return;
            }

            await destination.WriteAsync(Encoding.UTF8.GetBytes(inspected), cancellationToken).ConfigureAwait(false);
        }
    }
}
