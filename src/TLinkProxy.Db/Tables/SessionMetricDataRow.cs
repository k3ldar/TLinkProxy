using TLinkProxy.Base.Models;
using SimpleDB;

namespace TLinkProxy.Db.Tables
{
    [Table("SessionMetrics")]
    internal class SessionMetricDataRow : TableRowDefinition
    {
        string _model;
        string _sessionId;
        ObservableList<ToolInvocation> _toolInvocations;

        public SessionMetricDataRow()
        {
            _toolInvocations = [];
            _toolInvocations.Changed += ObservableDataChanged;
        }

        public string Model
        {
            get => _model;

            set
            {
                if (_model == value)
                    return;

                _model = value;
                Update();
            }
        }

        public string SessionId
        {
            get => _sessionId;

            set
            {
                if (_sessionId == value)
                    return;

                _sessionId = value;
                Update();
            }
        }

        public ObservableList<ToolInvocation> ToolInvocations
        {
            get => _toolInvocations;

            set
            {
                if (_toolInvocations == value)
                    return;

                _toolInvocations?.Changed -= ObservableDataChanged;

                _toolInvocations = value ?? [];
                _toolInvocations.Changed += ObservableDataChanged;
                Update();
            }
        }
    }
}
