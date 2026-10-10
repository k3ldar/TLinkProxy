using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base
{
    public static class Constants
    {
        /// <summary>
        /// Default JSON serializer options for deserializing requests. These options ignore null values, use a
        /// newline character for new lines, are case-insensitive for property names, and do not format
        /// the JSON with indentation. Additionally, it includes a converter to handle string representations of enums.
        /// </summary>
        public static readonly JsonSerializerOptions JsonRequestOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NewLine = NewLine,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        static Constants()
        {
            //JsonRequestOptions.Converters.Add(new JsonStringEnumConverter());
            //JsonRequestOptions.Converters.Add(new Converters.ArrayOrStringJsonConverter());
        }

        public const int MillisecondsPerSecond = 1000;
        public const int CharsPerToken = 4;
        public const string TodayFormat = "yyyy-MM-dd";
        public const string MonthFormat = "yyyy-MM";
        public const string LogFileExtension = ".dat";
        public const string GgufFileExtension = ".gguf";
        public const string WildcardFilePattern = "*";
        public const string LifetimeAggregateLabel = "all";

        /// <summary>Default per-request timeout (ms) when the routing envelope does not specify one.</summary>
        public const int DefaultRequestTimeoutMs = 60000;


        /// <summary>Environment variable names.</summary>
        public const string AiBackendsConfigEnvVar = "AI_BACKENDS_CONFIG";
        public const string RemoteOllamaUrlEnvVar = "REMOTE_OLLAMA_URL";
        public const string DeepSeekApiKeyEnvVar = "DEEPSEEK_API_KEY";
        public const string DeepSeekModelEnvVar = "DEEPSEEK_MODEL";

        /// <summary>Marker prefix for the reserved routing system message (fallback envelope transport).</summary>
        public const string RoutingSystemMessageMarker = "[[OLLAMAPROXY_ROUTING]]";

        /// <summary>Header used by clients to supply an explicit, pre-existing session id.</summary>
        public const string SessionIdHeader = "X-Session-Id";

        /// <summary>Authorization request header name.</summary>
        public const string AuthorizationHeader = "Authorization";

        /// <summary>User-Agent request header name.</summary>
        public const string UserAgentHeader = "User-Agent";

        /// <summary>Prefix marking a content-carried session id the model can echo into tool calls.</summary>
        public const string SessionMarkerPrefix = "__pfx_sid: ";

        /// <summary>Suffix terminating a content-carried session id marker.</summary>
        public const string SessionMarkerSuffix = "__";

        /// <summary>Change label recorded when a session marker is injected into the request.</summary>
        public const string SessionMarkerInjected = "session_marker_injected";


        public const string NewLine = "\n";
        public const string CrlfNewLine = "\r\n";
        public const string CarriageReturn = "\r";
        public const string Space = " ";

        public const char CharForwardSlash = '/';
        public const char CharUnderscore = '_';
        public const char CharDash = '-';
        public const char CharDot = '.';

        public const string MessageTypeRole = "role";
        public const string MessageConstant = "content";
        public const string ToolCallsPropertyName = "tool_calls";
        public const string ToolCallTypeFunction = "function";
        public const string DeltaPropertyName = "delta";
        public const string FinishReasonPropertyName = "finish_reason";
        public const string ChoicesPropertyName = "choices";
        public const string RoleTypeSystem = "system";
        public const string BackendClientName = "backend";
        public const string DefaultProxyUrl = "http://localhost:11434";
        public const string ProxyUrlEnvVar = "OLLAMAPROXY_URL";
        public const string ErrorRoutePath = "/Error";
        public const string DefaultRoutePattern = "{controller=Home}/{action=Index}/{id?}";
        public const string AppSettingsProxySessionLoggerSection = "ProxySessionLogger";
        public const string DefaultBackendConfigFileName = "ai-backends.json";
        public const string DefaultToolPolicyConfigFileName = "tool-policy.json";
        public const string ModelAliasRequiredMessage = "Alias is required.";
        public const string ModelKindRequiredMessage = "Kind is required.";
        public const string DefaultAppDataDirectoryName = "TLinkProxy";
        public const string LogsDirectoryName = "logs";
        public const string SessionLogFileExtension = ".log";
        public const string BinaryFileExtension = ".bin";
        public const string TextSummaryNull = "null";
        public const string BytesLabel = "bytes";
        public const string TruncatedSuffix = "...(truncated)";
        public const string FileNameSeparator = "-";
        public const string AuthenticationSchemeBearer = "Bearer";
        public const string JsonContentType = "application/json";
        public const string ModelPropertyName = "model";
        public const string RequestTypeCodeReview = "code-review";
        public const string RequestTypeTicketReview = "ticket-review";
        public const string RequestTypeChat = "chat";
        public const string RequestTypeAnalysis = "analysis";
        public const string RequestTypeFastResponse = "fast-response";
        public const string RequestTypeGeneralChat = "general-chat";
        public const string RequestTypeLongContext = "long-context";
        public const string RequestTypeToolCalling = "tool-calling";
        public const string ModelAliasMfDoom = "mfdoom";
        public const string ModelAliasQwen = "qwen";
        public const string ModelAliasLlamaLocal = "llama-local";
        public const string ModelAliasDeepSeek = "deepseek";
        public const string CapabilityGpu = "gpu";
        public const string CapabilityCpu = "cpu";
        public const string CapabilityToolCalling = "tool-calling";
        public const string CapabilityLongContext = "long-context";
        public const string LatencyFast = "fast";
        public const string LatencyMedium = "medium";
        public const string LatencySlow = "slow";
        public const string HealthPathOpenAI = "/v1/models";
        public const string HealthPathLlamaCpp = "/health";
        public const string HealthPathDefault = "/api/tags";
        public const string PathSeparator = "/";
        public const string ProxySessionLoggerService = "ProxySessionLogger";
        public const string TokenDataDirectoryName = "tokendata";
        public const string SessionMetricsDirectoryName = "sessionmetrics";
        public const string SessionMetricFileExtension = ".json";
        public const string DateTimeRoundTripFormat = "o";
        public const string EmptyString = "";
        public const string DefaultValueUnknown = "unknown";
        public const string CurrentDirectoryDot = ".";
        public const string DefaultRouteName = "default";
        public const string ConfigSettingModelPath = "ModelPath";
        public const string ConfigSettingLlamacpp = "Llamacpp";
        public const string ConfigSettingGlobalQueueCapacity = "GlobalQueueCapacity";
        public const string VisualStudioSettingsKey = "VisualStudioSettings";
        public const string VisualStudioSavedKey = "VisualStudioSaved";
        public const string JsonContentTypeToken = "json";
        public const string MaxCompletionTokensNormalized = "max_completion_tokens_normalized";
        public const string ToolChoiceCleared = "tool_choice_cleared";
        public const string CustomToolCallNormalized = "custom_tool_call_normalized";
        public const string OptimizerNoChangesMessage = "[Optimizer] No changes needed for GithubContext.";
        public const string ListSeparator = ", ";
        public const string TagSeparator = ",";
        public const string AzureKeyword = "azure";
        public const string UserRole = "user";
        public const string ToolRole = "tool";
        public const string BodyLengthOptimized = "Body length optimized";
        public const string RoutingKey = "routing";
        public const string RoutingExtractedTopLevel = "routing_extracted_top_level";
        public const string MessagesKey = "messages";
        public const string RoutingExtracted = "routing_extracted";
        public const string OpenAIBackendLabel = "OpenAIBackend";
        public const string LlamaCppBackendLabel = "LlamaCppBackend";
        public const string OllamaBackendLabel = "OllamaBackend";
        public const string ObjectTypeName = "object";
        public const string JsonPayloadType = "json";
        public const string OptimizerSerializeFailedFormat = "[Optimizer] Failed to serialize optimized GithubContext: {0}";
        public const string OptimizerAppliedChangesFormat = "[Optimizer] Applied changes: {0}";
        public const string OptimizerBytesSavedFormat = "Bytes Saved: {0}; Original Length: {1}; New Length: {2}";
        public const string OptimizerRemovedToolFormat = "Removed tool: {0}";
        public const string OptimizerToolConfigDisabledFormat = "tools[{0}].parameters.additionalProperties_disabled";
        public const string OptimizerMessageTruncatedFormat = "messages[{0}].content_truncated";
        public const string OptimizerBodyNormalizeFormat = "[Optimizer] Pre-optimized body by normalizing newlines. Original Length: {0}, New Length: {1}";
        public const string OptimizerExtractedTopLevelFormat = "[Optimizer] Extracted top-level routing envelope: {0}";
        public const string OptimizerFailedDeserializeFormat = "[Optimizer] Failed to deserialize modified OpenAIContext: {0}";
        public const string OptimizerFailedOpenAiContextFormat = "[Optimizer] Failed to deserialize OpenAIContext: {0}";
        public const string OptimizerModelSetFormat = "model_set:{0}";
        public const string GithubCopilotPromptPath = "C:\\ProgramData\\TLinkProxy\\GithubCopilotPrompt.txt";
        public const string DefaultProgramDataDirectory = "C:\\ProgramData\\TLinkProxy";
        public const string DefaultModelsDirectory = "C:\\ProgramData\\TLinkProxy\\Models";
        public const string DefaultLlamaCppDirectory = "C:\\ProgramData\\TLinkProxy\\LlamaCpp";
        public const string AiSettingsFileName = "TLinkProxy.json";
        public const int ShortMessageLengthThreshold = 10;
        public const int ConfigReloadDebounceMs = 250;

        public const int SecondsPerMinute = 60;
        public const int DefaultBackendHealthCheckSeconds = 30;
        public const string DefaultLlamaHost = "127.0.0.1";
        public const int DefaultLlamaPort = 8080;
        public const int DefaultLlamaContextSize = 8192;
        public const int DefaultLlamaHealthCheckSeconds = 15;
        public const string EnumValueRequiredMessage = "Enum value cannot be null or empty.";
        public const string IntegerValueRequiredMessage = "Integer value is required.";
        public const string FloatingPointValueRequiredMessage = "Floating-point value is required.";
        public const string BooleanValueRequiredMessage = "Boolean value is required.";
        public const int DefaultConcurrencyLimit = 1;
        public const int DefaultQueueDepth = 64;
        public const int DefaultGlobalQueueCapacity = 256;
        public const int DefaultRetryBackoffSeconds = 5;
        public const int DefaultProcessWaitSeconds = 60;
        public const int DefaultQueuePressureMultiplier = 5;
        public const int DefaultAvailableSlotsMultiplier = 3;
        public const int DefaultBackoffScaleSeconds = 10;
        public const int DefaultLongContextBufferTokens = 1024;
        public const int DefaultHttpTimeoutMinutes = 10;
        public const int DefaultHealthProbeSeconds = 5;
        public const int DefaultHealthProbeSecondsTimeout = 5;

        /// <summary>Maximum seconds to wait for an on-demand llama.cpp instance to start before giving up.</summary>
        public const int OnDemandStartWaitSeconds = 30;

        /// <summary>Poll interval (milliseconds) while waiting for an on-demand llama.cpp instance to start.</summary>
        public const int OnDemandStartPollMilliseconds = 50;

        /// <summary>Interval (seconds) used when polling the idle timeout of an on-demand llama.cpp instance.</summary>
        public const int OnDemandIdlePollSeconds = 1;
        public const int MaxTruncatedTextLength = 2000;
        public const int MaxTextControlThreshold = 512;
        public const int DefaultOutputTokenEstimate = 1024;
        public const int DefaultWeight = 50;
        public const int FastLatencyBonus = 30;
        public const int MediumLatencyBonus = 15;
        public const int SlowLatencyBonus = 0;
        public const int UnknownLatencyBonus = 10;
        public const int RequestTypeBonus = 25;
        public const int ToolCallingBonus = 10;
        public const int GpuBonus = 5;
        public const int CostPenaltyMultiplier = 10;
        public const int QueuePenaltyMultiplier = 5;
        public const int AvailableSlotsBonusMultiplier = 3;
        public const double CostPerThousandDivisor = 1000.0;
        public const int MinBinaryControlValue = 7;
        public const int MaxControlValue = 13;
        public const int MinPrintableAsciiValue = 32;
        public const double TextControlThresholdRatio = 0.3;


        public const string ConfigFileMissingInvalid = "Configuration file is missing or invalid.";
        public const string ModelDirectoryInvalid = "Models directory is not specified.";
        public const string LlamaCppExecutableNotFound = "llama.cpp executable is not specified.";

        /// <summary>Version string reported by the emulated native Ollama API.</summary>
        public const string OllamaApiVersion = "0.6.5";

        /// <summary>Backend path used when translating native Ollama chat/generate requests.</summary>
        public const string BackendPathChatCompletions = "/v1/chat/completions";

        /// <summary>Backend path used when translating native Ollama embedding requests.</summary>
        public const string BackendPathEmbeddings = "/v1/embeddings";

        /// <summary>Assistant role name used in Ollama chat responses.</summary>
        public const string OllamaAssistantRole = "assistant";

        /// <summary>Done reason reported when a completion finishes normally.</summary>
        public const string OllamaDoneReasonStop = "stop";

        /// <summary>Final status reported by stubbed pull/push/create endpoints.</summary>
        public const string OllamaStatusSuccess = "success";

        /// <summary>Initial status reported by the stubbed pull endpoint.</summary>
        public const string OllamaStatusPullingManifest = "pulling manifest";

        /// <summary>Initial status reported by the stubbed push endpoint.</summary>
        public const string OllamaStatusPushingManifest = "pushing manifest";

        /// <summary>Property name used to limit completion tokens in OpenAI-compatible requests.</summary>
        public const string MaxCompletionTokensPropertyName = "max_completion_tokens";

        /// <summary>Content type used for Ollama newline-delimited JSON streams.</summary>
        public const string NdjsonContentType = "application/x-ndjson";

        /// <summary>Error format used when a requested model cannot be resolved.</summary>
        public const string OllamaModelNotFoundErrorFormat = "model '{0}' not found";

        /// <summary>Default model format reported by the emulated native Ollama API.</summary>
        public const string OllamaDefaultModelFormat = "gguf";

        /// <summary>Compact format used when generating session identifiers.</summary>
        public const string GuidCompactFormat = "N";

        /// <summary>Number of nanoseconds in one millisecond, used for Ollama duration metrics.</summary>
        public const long NanosecondsPerMillisecond = 1000000L;

        /// <summary>Minutes until a loaded model is reported as expiring in /api/ps responses.</summary>
        public const int OllamaModelExpiryMinutes = 5;

        public const double OneMillionTokens = 1000000.0;
    }
}
