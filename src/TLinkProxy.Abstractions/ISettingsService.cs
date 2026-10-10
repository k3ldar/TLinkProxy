namespace TLinkProxy.Abstractions
{
    public interface ISettingsService
    {
        string GetSetting(string name);

        bool DeleteSetting(string name);

        string UpdateSetting(string name, string value);
    }
}
