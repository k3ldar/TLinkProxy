namespace TLinkProxy.Scheduling
{
    /// <summary>
    /// A request waiting for a concurrency slot in a backend's FIFO queue.
    /// </summary>
    public sealed class QueuedRequest
    {
        public QueuedRequest()
        {
            Turn = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>Completes when this request has been granted a concurrency slot.</summary>
        public TaskCompletionSource<bool> Turn { get; }
    }
}
