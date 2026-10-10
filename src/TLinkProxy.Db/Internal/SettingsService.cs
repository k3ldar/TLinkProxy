using TLinkProxy.Abstractions;
using TLinkProxy.Db.Tables;
using SimpleDB;

namespace TLinkProxy.Db.Internal
{
    internal sealed class SettingsService : ISettingsService
    {
        private const string ErrorSettingNameRequired = "Setting name is required.";
        private readonly ISimpleDBOperations<SettingsDataRow> _settingsData;

        public SettingsService(ISimpleDBOperations<SettingsDataRow> settingsData)
        {
            _settingsData = settingsData ?? throw new ArgumentNullException(nameof(settingsData));
        }

        public bool DeleteSetting(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            SettingsDataRow row = FindRow(name);

            if (row == null)
            {
                return false;
            }

            _settingsData.Delete(row);
            return true;
        }

        public string GetSetting(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            SettingsDataRow row = FindRow(name);
            return row?.Value;
        }

        public string UpdateSetting(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ErrorSettingNameRequired, nameof(name));
            }

            SettingsDataRow row = FindRow(name);

            if (row == null)
            {
                row = new SettingsDataRow { Name = name, Value = value };
                _settingsData.Insert(row);
                return value;
            }

            row.Value = value;
            _settingsData.Update(row);
            return value;
        }

        private SettingsDataRow FindRow(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            List<SettingsDataRow> settings = [.. _settingsData.Select(x => String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))];
            return settings.FirstOrDefault();
        }
    }
}
