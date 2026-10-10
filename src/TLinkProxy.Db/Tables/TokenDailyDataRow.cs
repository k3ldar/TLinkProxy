using TLinkProxy.Db.Internal;
using SimpleDB;
using System.Text.Json.Serialization;

namespace TLinkProxy.Db.Tables
{
    [Table("DailyTokenCounts")]
    internal class TokenDailyDataRow : TableRowDefinition
    {
        private int _requestCount;
        private long _originalTokensCount;
        private long _optimizedTokensCount;
        private ObservableDictionary<string, TokenStatsModel> _perModel = [];

        public TokenDailyDataRow()
        {
            _perModel = [];
            _perModel.Changed += ObservableDataChanged;
        }

        public int RequestCount
        {
            get => _requestCount;

            set
            {
                if (value == _requestCount)
                    return;

                _requestCount = value;
                Update();
            }
        }

        public long OriginalTokens
        {
            get => _originalTokensCount;

            set
            {
                if (value == _originalTokensCount)
                    return;

                _originalTokensCount = value;
                Update();
            }
        }

        public long OptimizedTokens
        {
            get => _optimizedTokensCount;

            set
            {
                if (value == _optimizedTokensCount)
                    return;

                _optimizedTokensCount = value;
                Update();
            }
        }

        [JsonInclude]
        public ObservableDictionary<string, TokenStatsModel> PerModel
        {
            get => _perModel;

            set
            {
                if (_perModel == value)
                    return;

                if (_perModel != null)
                    _perModel.Changed -= ObservableDataChanged;

                _perModel = value;
                _perModel.Changed += ObservableDataChanged;
                Update();
            }
        }
    }
}
