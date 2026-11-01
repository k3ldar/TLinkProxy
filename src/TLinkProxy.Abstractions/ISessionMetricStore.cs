namespace TLinkProxy.Abstractions
{
    public interface ISessionMetricStore
    {
        void Record(ISessionMetricRecord record);
        List<ISessionMetricRecord> GetAll();

        /// <summary>
        /// Returns the number of times each tool has been invoked across all sessions. The counts are
        /// maintained incrementally so this is cheap to call frequently (e.g. from dashboard polling).
        /// </summary>
        IReadOnlyDictionary<string, int> GetToolUsageCounts();
    }
}
