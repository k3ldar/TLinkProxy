namespace TLinkProxy.Abstractions
{
    public interface IModelScheduler
    {
        /// <summary>
        /// Acquire a slot for the backend identified by alias. Returns an <see cref="IDisposable"/>
        /// which must be disposed to release the slot, or <c>null</c> when the per-backend queue is
        /// full or the wait timed out.
        /// </summary>
        Task<IDisposable> AcquireAsync(string backendAlias, int timeoutMs, CancellationToken ct);

        /// <summary>Number of requests currently waiting in the backend's queue.</summary>
        int GetQueueDepth(string backendAlias);

        /// <summary>Number of currently available concurrency slots for the backend.</summary>
        int GetAvailableSlots(string backendAlias);

        /// <summary>
        /// Acquire a slot in the global back-pressure queue. Returns <c>null</c> immediately when
        /// the global queue is full (caller should return 429).
        /// </summary>
        Task<IDisposable> AcquireGlobalAsync(int timeoutMs, CancellationToken ct);
    }
}
