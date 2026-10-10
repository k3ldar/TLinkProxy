using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Models;
using TLinkProxy.Clients;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using static TLinkProxy.Base.Constants;

#pragma warning disable CC0009, CC0010

namespace TLinkProxy.Proxy.Controllers
{
    /// <summary>
    /// Emulates the native Ollama API surface. Model management endpoints (pull, push, create, delete)
    /// are stubs; all other endpoints return real data from the configured backends.
    /// </summary>
    [ApiController]
    [Route("api")]
    public sealed class OllamaController : Controller
    {
        private readonly IAiBackendRegistry _backendRegistry;
        private readonly BackendClientFactory _backendClientFactory;
        private readonly ILlamaCppProcessManager _llamaCppProcessManager;
        private readonly IProxySessionLogger _sessionLogger;
        private readonly ITokenStatsStore _tokenStatsStore;

        public OllamaController(IAiBackendRegistry backendRegistry, BackendClientFactory backendClientFactory,
            ILlamaCppProcessManager llamaCppProcessManager, IProxySessionLogger sessionLogger, ITokenStatsStore tokenStatsStore)
        {
            ArgumentNullException.ThrowIfNull(backendRegistry);
            ArgumentNullException.ThrowIfNull(backendClientFactory);
            ArgumentNullException.ThrowIfNull(llamaCppProcessManager);
            ArgumentNullException.ThrowIfNull(sessionLogger);
            ArgumentNullException.ThrowIfNull(tokenStatsStore);

            _backendRegistry = backendRegistry;
            _backendClientFactory = backendClientFactory;
            _llamaCppProcessManager = llamaCppProcessManager;
            _sessionLogger = sessionLogger;
            _tokenStatsStore = tokenStatsStore;
        }

        #region Public Methods

        [HttpPost("generate")]
        public async Task Generate()
        {
            await ExecuteCompletionAsync(false);
        }

        [HttpPost("chat")]
        public async Task Chat()
        {
            await ExecuteCompletionAsync(true);
        }

        [HttpPost("embeddings")]
        public async Task Embeddings()
        {
            string sessionId = GetSessionId();
            string requestBody = await ReadRequestBodyAsync();

            try
            {
                using JsonDocument document = JsonDocument.Parse(requestBody);
                JsonElement root = document.RootElement;

                string requestedModel = GetStringProperty(root, ModelPropertyName);
                string prompt = GetStringProperty(root, "prompt");

                if (String.IsNullOrWhiteSpace(prompt))
                {
                    prompt = GetStringProperty(root, "input");
                }

                IAiModelConfig selected = ResolveBackend(requestedModel);

                if (selected == null)
                {
                    await WriteModelNotFoundAsync(requestedModel);
                    return;
                }

                Dictionary<string, object> payload = new()
                {
                    [ModelPropertyName] = ResolveModelName(selected),
                    ["input"] = prompt ?? EmptyString
                };

                string backendBody = JsonSerializer.Serialize(payload, JsonRequestOptions);

                await _tokenStatsStore.RecordAsync(sessionId, requestBody.Length, backendBody.Length, ResolveModelName(selected));

                IBackendResponse backendResponse = await SendToBackendAsync(selected, BackendPathEmbeddings, backendBody, sessionId);

                using (backendResponse)
                {
                    string responseText = await backendResponse.Content.ReadAsStringAsync(HttpContext.RequestAborted);

                    if ((int)backendResponse.StatusCode >= (int)HttpStatusCode.BadRequest)
                    {
                        Response.StatusCode = (int)backendResponse.StatusCode;
                        Response.ContentType = JsonContentType;
                        await Response.WriteAsync(responseText, HttpContext.RequestAborted);
                        return;
                    }

                    using JsonDocument responseDocument = JsonDocument.Parse(responseText);
                    JsonElement embedding = responseDocument.RootElement
                        .GetProperty("data")[0]
                        .GetProperty("embedding");

                    var ollamaResponse = new
                    {
                        embedding
                    };

                    await WriteJsonAsync(ollamaResponse, JsonContentType);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await WriteBackendErrorAsync(sessionId, ex);
            }
        }

        [HttpGet("tags")]
        public IActionResult GetTags()
        {
            IReadOnlyList<IAiModelConfig> backends = _backendRegistry.GetAllBackends() ?? [];
            List<TagModel> modelsList = [];

            foreach (IAiModelConfig item in backends)
            {
                modelsList.Add(new TagModel
                {
                    name = item.Alias,
                    model = item.Model,
                    modified_at = DateTime.UtcNow.ToString(DateTimeRoundTripFormat),
                    size = 0L,
                    digest = EmptyString,
                    details = new TagDetails
                    {
                        parent_model = EmptyString,
                        format = OllamaDefaultModelFormat,
                        family = item.Kind,
                        families = item.Kind,
                        parameter_size = EmptyString,
                        quantization_level = EmptyString
                    }
                });
            }

            return Ok(new TagsModel { models = [.. modelsList] });
        }

        [HttpPost("show")]
        public async Task<IActionResult> Show()
        {
            string requestBody = await ReadRequestBodyAsync();
            string requestedModel = null;

            if (!String.IsNullOrWhiteSpace(requestBody))
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(requestBody);
                    requestedModel = GetStringProperty(document.RootElement, ModelPropertyName);

                    if (String.IsNullOrWhiteSpace(requestedModel))
                    {
                        requestedModel = GetStringProperty(document.RootElement, "name");
                    }
                }
                catch (JsonException)
                {
                    // fall through and report the model as not found
                }
            }

            IAiModelConfig config = FindBackend(requestedModel);

            if (config == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                var error = new { error = String.Format(OllamaModelNotFoundErrorFormat, requestedModel ?? EmptyString) };
                await WriteJsonAsync(error, JsonContentType);
                return new EmptyResult();
            }

            string family = config.Kind.ToString().ToLowerInvariant();

            var payload = new
            {
                license = EmptyString,
                modelfile = EmptyString,
                parameters = EmptyString,
                template = EmptyString,
                details = new
                {
                    parent_model = EmptyString,
                    format = OllamaDefaultModelFormat,
                    family,
                    families = new[] { family },
                    parameter_size = EmptyString,
                    quantization_level = EmptyString
                },
                model_info = new Dictionary<string, object>(),
                capabilities = new[] { "completion" }
            };

            return Ok(payload);
        }

        [HttpGet("ps")]
        public IActionResult GetPs()
        {
            IReadOnlyList<IAiModelConfig> backends = _backendRegistry.GetAllBackends() ?? [];
            List<object> running = [];

            foreach (IAiModelConfig item in backends)
            {
                DateTime? lastActivity = _llamaCppProcessManager.GetLastActivityUtc(item.Alias);
                bool isOnDemand = item.IsOnDemand || (item.Details != null && item.Details.TimeoutSeconds > 0);

                if (isOnDemand && !lastActivity.HasValue)
                {
                    continue;
                }

                DateTime expiresAt = (lastActivity ?? DateTime.UtcNow).AddMinutes(OllamaModelExpiryMinutes);

                running.Add(new
                {
                    name = item.Alias,
                    model = item.Model ?? item.Alias,
                    size = 0L,
                    digest = EmptyString,
                    details = new
                    {
                        parent_model = EmptyString,
                        format = OllamaDefaultModelFormat,
                        family = item.Kind.ToString().ToLowerInvariant(),
                        families = new[] { item.Kind.ToString().ToLowerInvariant() },
                        parameter_size = EmptyString,
                        quantization_level = EmptyString
                    },
                    expires_at = expiresAt.ToString(DateTimeRoundTripFormat),
                    size_vram = 0L
                });
            }

            return Ok(new { models = running });
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new { version = OllamaApiVersion });
        }

        [HttpPost("pull")]
        public async Task Pull()
        {
            await WriteStubStatusAsync(OllamaStatusPullingManifest);
        }

        [HttpPost("push")]
        public async Task Push()
        {
            await WriteStubStatusAsync(OllamaStatusPushingManifest);
        }

        [HttpPost("create")]
        public async Task Create()
        {
            await WriteStubStatusAsync(null);
        }

        [HttpDelete("delete")]
        public IActionResult Delete()
        {
            return Ok();
        }

        #endregion Public Methods

        #region Private Methods

        private async Task ExecuteCompletionAsync(bool isChat)
        {
            string sessionId = GetSessionId();
            string requestBody = await ReadRequestBodyAsync();
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                string backendBody;
                string requestedModel;
                bool stream;

                using (JsonDocument document = JsonDocument.Parse(requestBody))
                {
                    JsonElement root = document.RootElement;
                    requestedModel = GetStringProperty(root, ModelPropertyName);
                    stream = GetStreamFlag(root);

                    IAiModelConfig selected = ResolveBackend(requestedModel);

                    if (selected == null)
                    {
                        await WriteModelNotFoundAsync(requestedModel);
                        return;
                    }

                    backendBody = BuildChatCompletionsBody(root, selected, isChat);
                    requestedModel = ResolveModelName(selected);

                    await _tokenStatsStore.RecordAsync(sessionId, requestBody.Length, backendBody.Length, requestedModel);

                    IBackendResponse backendResponse = await SendToBackendAsync(selected, BackendPathChatCompletions, backendBody, sessionId);

                    using (backendResponse)
                    {
                        string responseText = await backendResponse.Content.ReadAsStringAsync(HttpContext.RequestAborted);

                        if ((int)backendResponse.StatusCode >= (int)HttpStatusCode.BadRequest)
                        {
                            Response.StatusCode = (int)backendResponse.StatusCode;
                            Response.ContentType = JsonContentType;
                            await Response.WriteAsync(responseText, HttpContext.RequestAborted);
                            return;
                        }

                        await WriteCompletionResponseAsync(requestedModel, responseText, isChat, stream, stopwatch);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await WriteBackendErrorAsync(sessionId, ex);
            }
        }

        private static string BuildChatCompletionsBody(JsonElement root, IAiModelConfig selected, bool isChat)
        {
            Dictionary<string, object> payload = new()
            {
                [ModelPropertyName] = ResolveModelName(selected),
                ["stream"] = false
            };

            if (isChat && root.TryGetProperty(MessagesKey, out JsonElement messages))
            {
                payload[MessagesKey] = messages;
            }
            else
            {
                string prompt = GetStringProperty(root, "prompt") ?? EmptyString;
                payload[MessagesKey] = new[] { new { role = UserRole, content = prompt } };
            }

            if (root.TryGetProperty("options", out JsonElement options) && options.ValueKind == JsonValueKind.Object)
            {
                if (options.TryGetProperty("temperature", out JsonElement temperature))
                {
                    payload["temperature"] = temperature;
                }

                if (options.TryGetProperty("top_p", out JsonElement topP))
                {
                    payload["top_p"] = topP;
                }

                if (options.TryGetProperty("num_predict", out JsonElement numPredict))
                {
                    payload[MaxCompletionTokensPropertyName] = numPredict;
                }
            }

            if (isChat && root.TryGetProperty("tools", out JsonElement tools))
            {
                payload["tools"] = tools;
            }

            return JsonSerializer.Serialize(payload, JsonRequestOptions);
        }

        private async Task WriteCompletionResponseAsync(string modelName, string openAiResponseText, bool isChat, bool stream, Stopwatch stopwatch)
        {
            string content = EmptyString;
            string doneReason = OllamaDoneReasonStop;
            long promptTokens = 0L;
            long completionTokens = 0L;

            using (JsonDocument document = JsonDocument.Parse(openAiResponseText))
            {
                JsonElement root = document.RootElement;

                if (root.TryGetProperty(ChoicesPropertyName, out JsonElement choices) && choices.GetArrayLength() > 0)
                {
                    JsonElement firstChoice = choices[0];

                    if (firstChoice.TryGetProperty("message", out JsonElement message) &&
                        message.TryGetProperty(MessageConstant, out JsonElement messageContent))
                    {
                        content = messageContent.GetString() ?? EmptyString;
                    }

                    if (firstChoice.TryGetProperty(FinishReasonPropertyName, out JsonElement finishReason))
                    {
                        doneReason = finishReason.GetString() ?? OllamaDoneReasonStop;
                    }
                }

                if (root.TryGetProperty("usage", out JsonElement usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out JsonElement promptElement))
                    {
                        promptTokens = promptElement.GetInt64();
                    }

                    if (usage.TryGetProperty("completion_tokens", out JsonElement completionElement))
                    {
                        completionTokens = completionElement.GetInt64();
                    }
                }
            }

            stopwatch.Stop();

            long elapsedNanoseconds = stopwatch.ElapsedMilliseconds * NanosecondsPerMillisecond;
            string createdAt = DateTime.UtcNow.ToString(DateTimeRoundTripFormat);

            if (stream)
            {
                Response.ContentType = NdjsonContentType;
                await Response.Body.WriteAsync(BuildCompletionChunk(modelName, createdAt, content, isChat, false), HttpContext.RequestAborted);
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(NewLine), HttpContext.RequestAborted);
                await Response.Body.WriteAsync(BuildCompletionFinal(modelName, createdAt, isChat, doneReason, elapsedNanoseconds, promptTokens, completionTokens), HttpContext.RequestAborted);
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(NewLine), HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
                return;
            }

            Response.ContentType = JsonContentType;
            await Response.Body.WriteAsync(BuildCompletionFinal(modelName, createdAt, isChat, doneReason, elapsedNanoseconds, promptTokens, completionTokens, content), HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }

        private static byte[] BuildCompletionChunk(string modelName, string createdAt, string content, bool isChat, bool done)
        {
            object chunk = isChat
                ? new
                {
                    model = modelName,
                    created_at = createdAt,
                    message = new { role = OllamaAssistantRole, content },
                    done
                }
                : new
                {
                    model = modelName,
                    created_at = createdAt,
                    response = content,
                    done
                };

            return JsonSerializer.SerializeToUtf8Bytes(chunk, JsonRequestOptions);
        }

        private static byte[] BuildCompletionFinal(string modelName, string createdAt, bool isChat, string doneReason,
            long totalDuration, long promptTokens, long completionTokens, string content = null)
        {
            object final = isChat
                ? new
                {
                    model = modelName,
                    created_at = createdAt,
                    message = new { role = OllamaAssistantRole, content = content ?? EmptyString },
                    done = true,
                    done_reason = doneReason,
                    total_duration = totalDuration,
                    load_duration = 0L,
                    prompt_eval_count = promptTokens,
                    prompt_eval_duration = 0L,
                    eval_count = completionTokens,
                    eval_duration = totalDuration
                }
                : new
                {
                    model = modelName,
                    created_at = createdAt,
                    response = content ?? EmptyString,
                    done = true,
                    done_reason = doneReason,
                    total_duration = totalDuration,
                    load_duration = 0L,
                    prompt_eval_count = promptTokens,
                    prompt_eval_duration = 0L,
                    eval_count = completionTokens,
                    eval_duration = totalDuration
                };

            return JsonSerializer.SerializeToUtf8Bytes(final, JsonRequestOptions);
        }

        private async Task WriteStubStatusAsync(string initialStatus)
        {
            string requestBody = await ReadRequestBodyAsync();
            bool stream = true;

            if (!String.IsNullOrWhiteSpace(requestBody))
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(requestBody);
                    stream = GetStreamFlag(document.RootElement);
                }
                catch (JsonException)
                {
                    // malformed bodies still succeed; the endpoints are stubs
                }
            }

            if (stream)
            {
                Response.ContentType = NdjsonContentType;

                if (initialStatus != null)
                {
                    await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { status = initialStatus }, JsonRequestOptions), HttpContext.RequestAborted);
                    await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(NewLine), HttpContext.RequestAborted);
                }

                await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { status = OllamaStatusSuccess }, JsonRequestOptions), HttpContext.RequestAborted);
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(NewLine), HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
                return;
            }

            Response.ContentType = JsonContentType;
            await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { status = OllamaStatusSuccess }, JsonRequestOptions), HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }

        private async Task<IBackendResponse> SendToBackendAsync(IAiModelConfig selected, string path, string body, string sessionId)
        {
            if (selected.IsOnDemand)
            {
                await _llamaCppProcessManager.EnsureStartedAsync(selected, HttpContext.RequestAborted);
            }

            await _sessionLogger.LogAsync(sessionId, $"[OllamaApi] Forwarding to backend '{selected.Alias}' path {path}");

            IBackendClient client = _backendClientFactory.CreateFor(selected);
            return await client.SendAsync(selected, path, body, sessionId, HttpContext.RequestAborted);
        }

        private IAiModelConfig ResolveBackend(string requestedModel)
        {
            IAiModelConfig match = FindBackend(requestedModel);

            if (match != null)
            {
                return match;
            }

            IReadOnlyList<IAiModelConfig> backends = _backendRegistry.GetAllBackends();

            if (backends == null || backends.Count == 0)
            {
                return null;
            }

            if (!String.IsNullOrWhiteSpace(requestedModel))
            {
                // Ollama would 404 here, but this proxy routes unknown models to the default backend.
                return backends.FirstOrDefault(b => b.Enabled) ?? backends[0];
            }

            return backends.FirstOrDefault(b => b.Enabled) ?? backends[0];
        }

        private IAiModelConfig FindBackend(string requestedModel)
        {
            if (String.IsNullOrWhiteSpace(requestedModel))
            {
                return null;
            }

            return _backendRegistry.GetAllBackends()?.FirstOrDefault(b =>
                (!String.IsNullOrWhiteSpace(b.Alias) && String.Equals(b.Alias, requestedModel, StringComparison.OrdinalIgnoreCase)) ||
                (!String.IsNullOrWhiteSpace(b.Model) && String.Equals(b.Model, requestedModel, StringComparison.OrdinalIgnoreCase)));
        }

        private static string ResolveModelName(IAiModelConfig selected)
        {
            return String.IsNullOrWhiteSpace(selected.Model) ? selected.Alias : selected.Model;
        }

        private string GetSessionId()
        {
            if (Request.Headers.TryGetValue(SessionIdHeader, out StringValues sessionId) && !String.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId.ToString();
            }

            return Guid.NewGuid().ToString(GuidCompactFormat);
        }

        private async Task<string> ReadRequestBodyAsync()
        {
            Request.EnableBuffering();

            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            using StreamReader reader = new(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            string body = await reader.ReadToEndAsync();

            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            return body;
        }

        private async Task WriteJsonAsync(object payload, string contentType)
        {
            string json = JsonSerializer.Serialize(payload, JsonRequestOptions);
            Response.ContentType = contentType;
            await Response.WriteAsync(json, HttpContext.RequestAborted);
        }

        private async Task WriteModelNotFoundAsync(string requestedModel)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            var error = new { error = String.Format(OllamaModelNotFoundErrorFormat, requestedModel ?? EmptyString) };
            await WriteJsonAsync(error, JsonContentType);
        }

        private async Task WriteBackendErrorAsync(string sessionId, Exception exception)
        {
            await _sessionLogger.LogAsync(sessionId, $"[OllamaApi] Backend call failed: {exception.Message}");

            if (Response.HasStarted)
            {
                return;
            }

            Response.StatusCode = (int)HttpStatusCode.BadGateway;
            var error = new { error = exception.Message };
            await WriteJsonAsync(error, JsonContentType);
        }

        private static string GetStringProperty(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            return null;
        }

        private static bool GetStreamFlag(JsonElement root)
        {
            if (root.TryGetProperty("stream", out JsonElement stream) &&
                (stream.ValueKind == JsonValueKind.True || stream.ValueKind == JsonValueKind.False))
            {
                return stream.GetBoolean();
            }

            return true;
        }

        #endregion Private Methods
    }
}

#pragma warning restore CC0009, CC0010
