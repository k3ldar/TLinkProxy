using TLinkProxy.Abstractions;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Models;
using Shared.Classes;
using System.Text.Json;
using System.Text.Json.Nodes;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Services
{
    public sealed class ToolCallPolicy : IToolCallPolicy, IDisposable
    {
        private const string RedactedValue = "[REDACTED]";
        private const string DefaultBlockMessage = "This tool call was blocked by policy.";

        private readonly object _lock = new();
        private readonly FileSystemWatcher _watcher;
        private readonly string _configFilePath;

        private ToolPolicySettings _settings = new();

        public ToolCallPolicy() : this(ResolveConfigFilePath())
        {
        }

        public ToolCallPolicy(string configFilePath)
        {
            ArgumentNullException.ThrowIfNull(configFilePath);
            _configFilePath = configFilePath;
            Load();

            string directory = Path.GetDirectoryName(_configFilePath) ?? CurrentDirectoryDot;
            _watcher = new FileSystemWatcher(directory, Path.GetFileName(_configFilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Renamed += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public IToolCallDecision Evaluate(IToolInvocation invocation)
        {
            if (invocation == null || String.IsNullOrWhiteSpace(invocation.Name))
            {
                return new ToolCallDecision { Action = ToolPolicyAction.Allow };
            }

            ToolPolicyRule rule;
            using (TimedLock.Lock(_lock))
            {
                rule = _settings.Rules?.FirstOrDefault(r => String.Equals(r.Name, invocation.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (rule == null || rule.Action == ToolPolicyAction.Allow)
            {
                return new ToolCallDecision { Action = ToolPolicyAction.Allow, SanitizedArguments = invocation.Arguments };
            }

            if (rule.Action == ToolPolicyAction.Block)
            {
                return new ToolCallDecision
                {
                    Action = ToolPolicyAction.Block,
                    Message = String.IsNullOrWhiteSpace(rule.Message) ? DefaultBlockMessage : rule.Message
                };
            }

            string sanitized = RedactSensitiveKeys(invocation.Arguments, rule.SensitiveKeys);
            return new ToolCallDecision
            {
                Action = ToolPolicyAction.Sanitize,
                SanitizedArguments = sanitized
            };
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Task.Delay(ConfigReloadDebounceMs).ContinueWith(_ => Load());
        }

        private void Load()
        {
            if (!File.Exists(_configFilePath))
            {
                lock (_lock)
                {
                    _settings = new ToolPolicySettings();
                }

                return;
            }

            try
            {
                string json = File.ReadAllText(_configFilePath);
                ToolPolicySettings settings = JsonSerializer.Deserialize<ToolPolicySettings>(json, JsonRequestOptions) ?? new ToolPolicySettings();

                lock (_lock)
                {
                    _settings = settings;
                }
            }
            catch
            {
                // keep previous settings on parse failure
            }
        }

        private static string RedactSensitiveKeys(string arguments, IReadOnlyCollection<string> sensitiveKeys)
        {
            if (String.IsNullOrWhiteSpace(arguments) || sensitiveKeys == null || sensitiveKeys.Count == 0)
            {
                return arguments;
            }

            try
            {
                JsonNode node = JsonNode.Parse(arguments);

                if (node is JsonObject obj)
                {
                    RedactObject(obj, sensitiveKeys);
                    return obj.ToJsonString(JsonRequestOptions);
                }
            }
            catch
            {
                // not valid JSON, leave unchanged
            }

            return arguments;
        }

        private static void RedactObject(JsonObject obj, IReadOnlyCollection<string> sensitiveKeys)
        {
            foreach (KeyValuePair<string, JsonNode> property in obj.ToList())
            {
                if (sensitiveKeys.Any(k => String.Equals(k, property.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    obj[property.Key] = RedactedValue;
                    continue;
                }

                if (property.Value is JsonObject nested)
                {
                    RedactObject(nested, sensitiveKeys);
                }
                else if (property.Value is JsonArray array)
                {
                    foreach (JsonNode item in array)
                    {
                        if (item is JsonObject arrayItem)
                        {
                            RedactObject(arrayItem, sensitiveKeys);
                        }
                    }
                }
            }
        }

        private static string ResolveConfigFilePath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string appdir = Path.Combine(dir, DefaultAppDataDirectoryName);
            Directory.CreateDirectory(appdir);
            return Path.Combine(appdir, DefaultToolPolicyConfigFileName);
        }
    }
}
