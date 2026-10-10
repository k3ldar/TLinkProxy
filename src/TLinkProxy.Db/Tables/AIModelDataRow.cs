using TLinkProxy.Abstractions;
using SimpleDB;

namespace TLinkProxy.Db.Tables
{
    [Table("AIModels")]
    internal class AIModelDataRow : TableRowDefinition
    {
        private string _alias;
        private ModelKind _kind;
        private string _endpoint;
        private string _model;
        private ObservableList<string> _tags;
        private ModelComputeProfile _computeType;
        private ModelLatencyProfile _latencyProfile;
        private decimal _costPerMillionTokensInput;
        private decimal _costPerMillionTokensOutput;
        private ModelMaximumContextSize _maxContextSize;
        private byte _concurrencyLimit;
        private byte _weight;
        private bool _allowsQueue;
        private byte _maxQueueDepth;
        private bool _isEnabled;
        private string _apiKeyEnvVar;
        private uint _healthProbeSeconds;
        private uint _gpuLayers;
        private byte _threads;
        private uint _batchSize;
        private bool _autoRestart;
        private uint _timeoutSeconds;

        public AIModelDataRow()
        {
            _tags = [];
            _tags.Changed += ObservableDataChanged;
        }

        public string Alias
        {
            get => _alias;

            set
            {
                if (value == _alias)
                    return;

                _alias = value;
                Update();
            }
        }

        public ModelKind Kind
        {
            get => _kind;

            set
            {
                if (value == _kind)
                    return;

                _kind = value;
                Update();
            }
        }

        public string Endpoint
        {
            get => _endpoint;

            set
            {
                if (value == _endpoint)
                    return;

                _endpoint = value;
                Update();
            }
        }

        public string Model
        {
            get => _model;

            set
            {
                if (value == _model)
                    return;

                _model = value;
                Update();
            }
        }

        public ObservableList<string> Tags
        {
            get => _tags;

            set
            {
                if (value == _tags)
                    return;

                _tags?.Changed -= ObservableDataChanged;

                _tags = value;
                _tags.Changed += ObservableDataChanged;
                Update();
            }
        }

        public ModelComputeProfile ComputeType
        {
            get => _computeType;

            set
            {
                if (value == _computeType)
                    return;

                _computeType = value;
                Update();
            }
        }

        public ModelLatencyProfile LatencyProfile
        {
            get => _latencyProfile;

            set
            {
                if (value == _latencyProfile)
                    return;

                _latencyProfile = value;
                Update();
            }
        }

        public decimal CostPerMillionTokensInput
        {
            get => _costPerMillionTokensInput;

            set
            {
                if (value == _costPerMillionTokensInput)
                    return;

                _costPerMillionTokensInput = value;
                Update();
            }
        }

        public decimal CostPerMillionTokensOutput
        {
            get => _costPerMillionTokensOutput;

            set
            {
                if (value == _costPerMillionTokensOutput)
                    return;

                _costPerMillionTokensOutput = value;
                Update();
            }
        }

        public ModelMaximumContextSize MaxContextSize
        {
            get => _maxContextSize;

            set
            {
                if (value == _maxContextSize)
                    return;

                _maxContextSize = value;
                Update();
            }
        }

        public byte ConcurrencyLimit
        {
            get => _concurrencyLimit;

            set
            {
                if (value == _concurrencyLimit)
                    return;

                _concurrencyLimit = value;
                Update();
            }
        }

        public byte Weight
        {
            get => _weight;

            set
            {
                if (value == _weight)
                    return;

                _weight = value;
                Update();
            }
        }

        public bool AllowsQueue
        {
            get => _allowsQueue;

            set
            {
                if (value == _allowsQueue)
                    return;

                _allowsQueue = value;
                Update();
            }
        }

        public byte MaxQueueDepth
        {
            get => _maxQueueDepth;

            set
            {
                if (value == _maxQueueDepth)
                    return;

                _maxQueueDepth = value;
                Update();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;

            set
            {
                if (value == _isEnabled)
                    return;

                _isEnabled = value;
                Update();
            }
        }

        public string ApiKeyEnvVar
        {
            get => _apiKeyEnvVar;

            set
            {
                if (value == _apiKeyEnvVar)
                    return;

                _apiKeyEnvVar = value;
                Update();
            }
        }

        public uint HealthProbeSeconds
        {
            get => _healthProbeSeconds;

            set
            {
                if (value == _healthProbeSeconds)
                    return;

                _healthProbeSeconds = value;
                Update();
            }
        }

        public uint GpuLayers
        {
            get => _gpuLayers;

            set
            {
                if (value == _gpuLayers)
                    return;

                _gpuLayers = value;
                Update();
            }
        }

        public byte Threads
        {
            get => _threads;

            set
            {
                if (value == _threads)
                    return;

                _threads = value;
                Update();
            }
        }

        public uint BatchSize
        {
            get => _batchSize;

            set
            {
                if (value == _batchSize)
                    return;

                _batchSize = value;
                Update();
            }
        }

        public bool AutoRestart
        {
            get => _autoRestart;

            set
            {
                if (value == _autoRestart)
                    return;

                _autoRestart = value;
                Update();
            }
        }

        public uint TimeoutSeconds
        {
            get => _timeoutSeconds;

            set
            {
                if (value == _timeoutSeconds)
                    return;

                _timeoutSeconds = value;
                Update();
            }
        }
    }
}
