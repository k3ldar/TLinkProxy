using System.Buffers;

#pragma warning disable CC0009, CC0010

namespace TLinkProxy.Proxy.Streaming
{
    /// <summary>
    /// Growable byte buffer backed by <see cref="ArrayPool{T}"/> so transient response data does not
    /// create large short-lived arrays that end up on the large object heap.
    /// </summary>
    internal sealed class PooledByteBuffer : IDisposable
    {
        private const int DefaultCapacity = 8192;

        private byte[] _array;
        private int _length;

        public PooledByteBuffer(int initialCapacity = DefaultCapacity)
        {
            _array = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 1));
        }

        public int Length => _length;

        public ReadOnlySpan<byte> Span => _array.AsSpan(0, _length);

        public ReadOnlyMemory<byte> Memory => _array.AsMemory(0, _length);

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            EnsureCapacity(_length + data.Length);
            data.CopyTo(_array.AsSpan(_length));
            _length += data.Length;
        }

        /// <summary>Removes <paramref name="count"/> bytes from the start of the buffer.</summary>
        public void Consume(int count)
        {
            if (count >= _length)
            {
                _length = 0;
                return;
            }

            _array.AsSpan(count, _length - count).CopyTo(_array);
            _length -= count;
        }

        public void Dispose()
        {
            byte[] array = _array;
            _array = null;

            if (array != null)
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _array.Length)
            {
                return;
            }

            byte[] larger = ArrayPool<byte>.Shared.Rent(Math.Max(required, _array.Length * 2));
            _array.AsSpan(0, _length).CopyTo(larger);
            ArrayPool<byte>.Shared.Return(_array);
            _array = larger;
        }
    }
}
