namespace TLinkProxy.Abstractions
{
    public interface ITokenStatsStore
    {
        Task RecordAsync(string sessionId, int originalBytes, int optimizedBytes, string modelName);
        ITokenDailyRecord GetToday();
        ITokenDailyRecord GetThisMonth();
        ITokenDailyRecord GetLifetime();
    }
}
