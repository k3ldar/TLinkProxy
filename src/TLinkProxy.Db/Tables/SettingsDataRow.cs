using SimpleDB;

namespace TLinkProxy.Db.Tables
{
    [Table("Settings")]
    internal class SettingsDataRow : TableRowDefinition
    {
        private string _name;
        private string _value;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                Update();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                _value = value;
                Update();
            }
        }
    }
}
