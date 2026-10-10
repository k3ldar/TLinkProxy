using TLinkProxy.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    public sealed class AiSettings : IAiSettings
    {
        private const string LlamaExeName = "llama-server.exe";

        public AiSettings()
        {
            ModelPath = Base.Constants.DefaultModelsDirectory;
            Llamacpp = Path.Combine(Base.Constants.DefaultLlamaCppDirectory, LlamaExeName);
        }

        /// <summary>Shared directory containing llama-cpp model files, used as a fallback when a backend omits its model path.</summary>
        [JsonPropertyName("modelPath")]
        public string ModelPath { get; set; }

        /// <summary>Shared path to the llama-server executable, used as a fallback when a backend omits its executable.</summary>
        [JsonPropertyName("llamacpp")]
        public string Llamacpp { get; set; }

        /// <summary>Maximum number of requests waiting in the global back-pressure queue across all backends.</summary>
        [JsonPropertyName("globalQueueCapacity")]
        public int GlobalQueueCapacity { get; set; } = Base.Constants.DefaultGlobalQueueCapacity;

        public bool LogToFile { get; set; } = true;

        public static AiSettings Load()
        {
            return Load(Path.Combine(Base.Constants.DefaultProgramDataDirectory, Base.Constants.AiSettingsFileName));
        }

        public static AiSettings Load(string configPath)
        {
            AiSettings settings = new();

            if (String.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            {
                return settings;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                AiSettings loaded = JsonSerializer.Deserialize<AiSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (loaded != null)
                {
                    settings = loaded;
                }
            }
            catch (JsonException)
            {
                // invalid json, fall back to defaults
            }
            catch (IOException)
            {
                // unreadable file, fall back to defaults
            }
            catch (UnauthorizedAccessException)
            {
                // access denied, fall back to defaults
            }

            return settings;
        }
    }
}
