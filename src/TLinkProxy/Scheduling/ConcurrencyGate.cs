using System.Threading.Channels;

namespace TLinkProxy.Scheduling
{
    /// <summary>
    /// Enforces per-backend concurrency via a <see cref="SemaphoreSlim"/> plus a bounded FIFO
    /// queue. A single background drainer hands a slot to the next queued request as soon as
    /// one frees up, preserving FIFO ordering.
    /// </summary>
    public sealed class ConcurrencyGate : IDisposable
    {
        private readonly SemaphoreSlim _slots;
        private readonly Channel<QueuedRequest> _queue;
        private readonly Task _drainer;
        private int _disposed;

        public ConcurrencyGate(int concurrencyLimit, int queueCapacity)
        {
            concurrencyLimit = Math.Max(1, concurrencyLimit);
            queueCapacity = Math.Max(0, queueCapacity);

            _slots = new SemaphoreSlim(concurrencyLimit, concurrencyLimit);
            _queue = Channel.CreateBounded<QueuedRequest>(new BoundedChannelOptions(Math.Max(1, queueCapacity))
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });

            if (queueCapacity > 0)
            {
                _drainer = Task.Run(DrainLoopAsync);
            }
            else
            {
                _drainer = Task.CompletedTask;
            }
        }

        public int AvailableSlots => Math.Max(0, _slots.CurrentCount);

        public int QueueDepth => Math.Max(0, _queue.Reader.Count);

        /// <summary>
        /// Attempts to acquire a slot. Returns an <see cref="IDisposable"/> that must be disposed
        /// to release the slot, or <c>null</c> when the queue is full or the wait timed out.
        /// </summary>
        public async Task<IDisposable> AcquireAsync(int timeoutMs, CancellationToken ct)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return null;
            }

            // Fast path: a slot is immediately available.
            if (await _slots.WaitAsync(0, ct).ConfigureAwait(false))
            {
                return new Releaser(_slots);
            }

            // No free slot; enqueue ourselves in the bounded FIFO queue.
            QueuedRequest queued = new();

            if (!_queue.Writer.TryWrite(queued))
            {
                return null; // queue full -> caller should fall back or reject
            }

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            if (timeoutMs > 0)
            {
                timeoutCts.CancelAfter(timeoutMs);
            }

            try
            {
                // The drainer reserves a slot and completes our Turn.
                await queued.Turn.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                return new Releaser(_slots);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private async Task DrainLoopAsync()
        {
            try
            {
                while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (_queue.Reader.TryRead(out QueuedRequest queued))
                    {
                        if (Volatile.Read(ref _disposed) != 0)
                        {
                            return;
                        }

                        // Reserve a slot on behalf of the queued request.
                        await _slots.WaitAsync(Timeout.InfiniteTimeSpan).ConfigureAwait(false);

                        if (Volatile.Read(ref _disposed) != 0)
                        {
                            try { _slots.Release(); } catch { }
                            return;
                        }

                        queued.Turn.TrySetResult(true);
                        break; // one slot granted per freed slot
                    }
                }
            }
            catch
            {
                // drainer stopped (disposed)
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try { _queue.Writer.TryComplete(); } catch { }
            try { _slots.Dispose(); } catch { }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _slots;
            private int _released;

            public Releaser(SemaphoreSlim slots) => _slots = slots;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    try { _slots?.Release(); } catch { }
                }
            }
        }
    }
}
