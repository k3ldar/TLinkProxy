using System.Text;

using TLinkProxy.Proxy.Streaming;

namespace TLinkProxy.Tests.Services
{
    [TestClass]
    public sealed class SseStreamRelayTests
    {
        private const string PlainEvent1 = "data: {\"choices\":[{\"delta\":{\"content\":\"Hi\"}}]}\n\n";
        private const string PlainEvent2 = "data: {\"choices\":[{\"delta\":{\"content\":\" there\"}}]}\n\n";
        private const string ToolEvent = "data: {\"choices\":[{\"delta\":{\"tool_calls\":[{\"index\":0,\"function\":{\"name\":\"x\"}}]}}]}\n\n";
        private const string DoneEvent = "data: [DONE]\n\n";

        [TestMethod]
        public async Task Relay_NoToolCalls_PassesThroughWithoutInspecting()
        {
            string input = PlainEvent1 + PlainEvent2 + DoneEvent;
            bool inspected = false;

            string output = await RunAsync(new ChunkedStream(input, 7), _ =>
            {
                inspected = true;
                return Task.FromResult("ignored");
            });

            Assert.AreEqual(input, output);
            Assert.IsFalse(inspected);
        }

        [TestMethod]
        public async Task Relay_EventsBeforeToolCall_AreForwardedBeforeStreamEnds()
        {
            string input = PlainEvent1 + ToolEvent + DoneEvent;
            using TrackingStream destination = new();
            string inspectedText = null;

            await SseStreamRelay.RelayAsync(new ChunkedStream(input, 4096), destination, text =>
            {
                inspectedText = text;
                return Task.FromResult(text);
            }, null, CancellationToken.None);

            Assert.AreEqual(PlainEvent1, destination.FirstWrite);
            Assert.AreEqual(ToolEvent + DoneEvent, inspectedText);
            Assert.AreEqual(input, destination.Text);
        }

        [TestMethod]
        public async Task Relay_ToolCallsRewritten_WritesInspectedText()
        {
            string input = PlainEvent1 + ToolEvent + DoneEvent;

            string output = await RunAsync(new ChunkedStream(input, 5), _ => Task.FromResult("data: rewritten\n\n"));

            Assert.AreEqual(PlainEvent1 + "data: rewritten\n\n", output);
        }

        [TestMethod]
        public async Task Relay_ToolCallsUnchanged_WritesOriginalBytes()
        {
            string input = ToolEvent + DoneEvent;

            string output = await RunAsync(new ChunkedStream(input, 11), text => Task.FromResult(text));

            Assert.AreEqual(input, output);
        }

        [TestMethod]
        public async Task Relay_CrLfEventBoundaries_AreRecognised()
        {
            string input = "data: one\r\n\r\ndata: two\r\n\r\n";
            using TrackingStream destination = new();

            await SseStreamRelay.RelayAsync(new ChunkedStream(input, 3), destination, null, null, CancellationToken.None);

            Assert.AreEqual(input, destination.Text);
            Assert.IsGreaterThan(1, destination.WriteCount);
        }

        [TestMethod]
        public async Task Relay_IncompleteTrailingEvent_IsStillWritten()
        {
            string input = PlainEvent1 + "data: partial";

            string output = await RunAsync(new ChunkedStream(input, 6), null);

            Assert.AreEqual(input, output);
        }

        [TestMethod]
        public async Task Relay_ChunkCallback_ReceivesEveryChunk()
        {
            string input = PlainEvent1 + PlainEvent2;
            StringBuilder seen = new();

            using MemoryStream destination = new();
            await SseStreamRelay.RelayAsync(new ChunkedStream(input, 10), destination, null, chunk =>
            {
                seen.Append(Encoding.UTF8.GetString(chunk.Span));
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.AreEqual(input, seen.ToString());
        }

        [TestMethod]
        public async Task Relay_LargeToolCallPayload_IsHeldAndReturnedIntact()
        {
            string bigArgs = new('a', 50_000);
            string toolEvent = "data: {\"tool_calls\":[{\"arguments\":\"" + bigArgs + "\"}]}\n\n";
            string input = PlainEvent1 + toolEvent + DoneEvent;

            string output = await RunAsync(new ChunkedStream(input, 8192), text => Task.FromResult(text));

            Assert.AreEqual(input, output);
        }

        [TestMethod]
        public void ContainsToolCalls_NullOrEmptyArray_ReturnsFalse()
        {
            Assert.IsFalse(SseStreamRelay.ContainsToolCalls("data: {\"tool_calls\":null}\n\n"u8));
            Assert.IsFalse(SseStreamRelay.ContainsToolCalls("data: {\"tool_calls\": [ ]}\n\n"u8));
            Assert.IsFalse(SseStreamRelay.ContainsToolCalls("data: {\"content\":\"hello\"}\n\n"u8));
        }

        [TestMethod]
        public void ContainsToolCalls_PopulatedArray_ReturnsTrue()
        {
            Assert.IsTrue(SseStreamRelay.ContainsToolCalls("data: {\"tool_calls\": [{\"id\":\"1\"}]}\n\n"u8));
        }

        [TestMethod]
        public void FindEventLength_NoBoundary_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, SseStreamRelay.FindEventLength("data: x\n"u8));
        }

        [TestMethod]
        public void FindEventLength_Boundary_ReturnsLengthIncludingTerminator()
        {
            Assert.AreEqual(9, SseStreamRelay.FindEventLength("data: x\n\ndata: y\n\n"u8));        }

        private static async Task<string> RunAsync(Stream source, Func<string, Task<string>> inspect)
        {
            using MemoryStream destination = new();
            await SseStreamRelay.RelayAsync(source, destination, inspect, null, CancellationToken.None);
            return Encoding.UTF8.GetString(destination.ToArray());
        }

        /// <summary>Returns the data in fixed-size pieces to simulate network chunking.</summary>
        private sealed class ChunkedStream(string text, int chunkSize) : Stream
        {
            private readonly byte[] _data = Encoding.UTF8.GetBytes(text);
            private int _position;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> buffer)
            {
                int n = Math.Min(Math.Min(chunkSize, buffer.Length), _data.Length - _position);
                _data.AsSpan(_position, n).CopyTo(buffer);
                _position += n;
                return n;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                => new(Read(buffer.Span));

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class TrackingStream : Stream
        {
            private readonly MemoryStream _inner = new();

            public string FirstWrite { get; private set; }
            public int WriteCount { get; private set; }
            public string Text => Encoding.UTF8.GetString(_inner.ToArray());

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

            public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                FirstWrite ??= Encoding.UTF8.GetString(buffer);
                WriteCount++;
                _inner.Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Write(buffer.Span);
                return ValueTask.CompletedTask;
            }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
