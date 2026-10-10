namespace TLinkProxy.Abstractions
{
    public interface ISessionMetricStore
    {
        void Record(ISessionMetricRecord record);
        List<ISessionMetricRecord> GetAll();
    }
}
