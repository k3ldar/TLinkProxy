using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using TLinkProxy.Kernel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Services
{
    public sealed class ToolCallInspector : IToolCallInspector
    {
        private const string SseDataPrefix = "data:";
        private const string SseDone = "[DONE]";
        private const string FinishReasonToolCalls = "tool_calls";
        private const string FinishReasonStop = "stop";

        private readonly IToolCallPolicy _policy;
        private readonly ISessionMetricStore _metricStore;

        public ToolCallInspector(IToolCallPolicy policy, ISessionMetricStore metricStore)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _metricStore = metricStore ?? throw new ArgumentNullException(nameof(metricStore));
        }

        public async Task<string> InspectResponseAsync(string responseBody, string sessionId, string fallbackModel, bool isStreaming, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(responseBody))
            {
                return responseBody;
            }

            return isStreaming
                ? await InspectStreamingAsync(responseBody, sessionId, fallbackModel, cancellationToken).ConfigureAwait(false)
                : await InspectNonStreamingAsync(responseBody, sessionId, fallbackModel, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> InspectNonStreamingAsync(string responseBody, string sessionId, string fallbackModel, CancellationToken cancellationToken)
        {
            ChatCompletionResponse response;
            try
            {
                response = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, JsonRequestOptions);
            }
            catch
            {
                return responseBody;
            }

            if (response == null || response.Choices == null)
            {
                return responseBody;
            }

            string model = String.IsNullOrWhiteSpace(response.Model) ? fallbackModel : response.Model;
            List<IToolInvocation> allInvocations = [];
            bool changed = false;

            foreach (ChatCompletionChoice choice in response.Choices)
            {
                if (choice?.Message?.ToolCalls == null || choice.Message.ToolCalls.Count == 0)
                {
                    continue;
                }

                (List<ToolCall> kept, List<string> blockMessages, List<IToolInvocation> invocations, bool choiceChanged) =
                    ApplyPolicy(choice.Message.ToolCalls, allInvocations);

                changed |= choiceChanged;
                choice.Message.ToolCalls = kept;

                if (blockMessages.Count > 0)
                {
                    choice.Message.Content = AppendBlockMessages(choice.Message.Content, blockMessages);

                    if (kept.Count == 0)
                    {
                        choice.FinishReason = FinishReasonStop;
                    }

                    changed = true;
                }
            }

            await RecordMetricsAsync(sessionId, model, allInvocations, cancellationToken).ConfigureAwait(false);

            return changed ? JsonSerializer.Serialize(response, JsonRequestOptions) : responseBody;
        }

        private async Task<string> InspectStreamingAsync(string responseBody, string sessionId, string fallbackModel, CancellationToken cancellationToken)
        {
            (Message merged, string model, bool parsed) = MergeStream(responseBody, fallbackModel);

            if (!parsed || merged.ToolCalls == null || merged.ToolCalls.Count == 0)
            {
                return responseBody;
            }

            List<IToolInvocation> allInvocations = [];
            (List<ToolCall> kept, List<string> blockMessages, _, bool changed) =
                ApplyPolicy(merged.ToolCalls, allInvocations);

            await RecordMetricsAsync(sessionId, model, allInvocations, cancellationToken).ConfigureAwait(false);

            if (!changed)
            {
                return responseBody;
            }

            merged.ToolCalls = kept;

            if (blockMessages.Count > 0)
            {
                merged.Content = AppendBlockMessages(merged.Content, blockMessages);
            }

            return RebuildSse(merged, kept.Count > 0 ? FinishReasonToolCalls : FinishReasonStop, model);
        }

        private (List<ToolCall> Kept, List<string> BlockMessages, List<IToolInvocation> Invocations, bool Changed) ApplyPolicy(
            List<ToolCall> toolCalls, List<IToolInvocation> allInvocations)
        {
            List<ToolCall> kept = [];
            List<string> blockMessages = [];
            List<IToolInvocation> invocations = [];
            bool changed = false;

            foreach (ToolCall call in toolCalls)
            {
                ToolInvocation invocation = new()
                {
                    Name = call?.Name ?? DefaultValueUnknown,
                    Arguments = call?.Arguments ?? String.Empty
                };

                IToolCallDecision decision = _policy.Evaluate(invocation);

                switch (decision.Action)
                {
                    case ToolPolicyAction.Block:
                        invocation.Outcome = ToolInvocationOutcome.Blocked;
                        blockMessages.Add(decision.Message);
                        changed = true;
                        break;

                    case ToolPolicyAction.Sanitize:
                        invocation.Outcome = ToolInvocationOutcome.Sanitized;
                        invocation.Arguments = decision.SanitizedArguments;

                        if (call.Function != null)
                        {
                            call.Function.Arguments = decision.SanitizedArguments;
                        }
                        else if (call.Custom != null)
                        {
                            call.Custom.Input = decision.SanitizedArguments;
                        }

                        kept.Add(call);
                        changed = true;
                        break;

                    default:
                        invocation.Outcome = ToolInvocationOutcome.Allowed;
                        kept.Add(call);
                        break;
                }

                invocations.Add(invocation);
            }

            allInvocations.AddRange(invocations);
            return (kept, blockMessages, invocations, changed);
        }

        private async Task RecordMetricsAsync(string sessionId, string model, List<IToolInvocation> invocations, CancellationToken cancellationToken)
        {
            if (invocations == null || invocations.Count == 0)
            {
                return;
            }

            SessionMetricRecord record = new()
            {
                SessionId = sessionId ?? String.Empty,
                RecordedAtUtc = DateTime.UtcNow,
                Model = model ?? DefaultValueUnknown,
                ToolInvocations = invocations
            };

            _metricStore.Record(record);
        }

        private static string AppendBlockMessages(string content, List<string> blockMessages)
        {
            string joined = String.Join(NewLine, blockMessages);
            string existing = content ?? String.Empty;

            return existing.Length == 0 ? joined : existing + NewLine + joined;
        }

        private static (Message Message, string Model, bool Parsed) MergeStream(string sseText, string fallbackModel)
        {
            Message merged = new();
            StringBuilder content = new();
            Dictionary<int, ToolCall> toolCalls = [];
            string model = fallbackModel;
            bool parsed = false;

            foreach (string line in sseText.Split(NewLine))
            {
                string trimmed = line.Trim();

                if (!trimmed.StartsWith(SseDataPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string json = trimmed[SseDataPrefix.Length..].Trim();

                if (String.IsNullOrEmpty(json) || String.Equals(json, SseDone, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ChatCompletionResponse chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<ChatCompletionResponse>(json, JsonRequestOptions);
                }
                catch
                {
                    continue;
                }

                if (chunk == null)
                {
                    continue;
                }

                parsed = true;

                if (!String.IsNullOrWhiteSpace(chunk.Model))
                {
                    model = chunk.Model;
                }

                if (chunk.Choices == null)
                {
                    continue;
                }

                foreach (ChatCompletionChoice choice in chunk.Choices)
                {
                    if (choice?.Delta == null)
                    {
                        continue;
                    }

                    if (!String.IsNullOrWhiteSpace(choice.Delta.Content))
                    {
                        content.Append(choice.Delta.Content);
                    }

                    if (choice.Delta.ToolCalls == null)
                    {
                        continue;
                    }

                    foreach (ToolCall call in choice.Delta.ToolCalls)
                    {
                        int index = call.Index ?? 0;

                        if (!toolCalls.TryGetValue(index, out ToolCall existing))
                        {
                            toolCalls[index] = call;
                            continue;
                        }

                        existing.Id ??= call.Id;
                        existing.Type ??= call.Type;

                        if (call.Function != null)
                        {
                            existing.Function ??= call.Function;

                            if (existing.Function != call.Function)
                            {
                                existing.Function.Name ??= call.Function.Name;
                                existing.Function.Arguments += call.Function.Arguments ?? String.Empty;
                            }
                        }
                        else if (call.Custom != null)
                        {
                            existing.Custom ??= call.Custom;

                            if (existing.Custom != call.Custom)
                            {
                                existing.Custom.Name ??= call.Custom.Name;
                                existing.Custom.Input += call.Custom.Input ?? String.Empty;
                            }
                        }
                    }
                }
            }

            merged.Content = content.Length == 0 ? null : content.ToString();
            merged.ToolCalls = [.. toolCalls.Values.OrderBy(t => t.Index ?? 0)];

            return (merged, model, parsed);
        }

        private static string RebuildSse(Message message, string finishReason, string model)
        {
            JsonObject delta = [];

            if (!String.IsNullOrWhiteSpace(message.Content))
            {
                delta[MessageConstant] = message.Content;
            }

            if (message.ToolCalls != null && message.ToolCalls.Count > 0)
            {
                JsonArray toolCallsArray = [];

                foreach (ToolCall call in message.ToolCalls)
                {
                    toolCallsArray.Add(JsonNode.Parse(JsonSerializer.Serialize(call, JsonRequestOptions)));
                }

                delta[ToolCallsPropertyName] = toolCallsArray;
            }

            JsonObject choice = new()
            {
                [DeltaPropertyName] = delta,
                [FinishReasonPropertyName] = finishReason
            };

            JsonObject chunk = new()
            {
                [ChoicesPropertyName] = new JsonArray(new JsonObject[] { choice })
            };

            if (!String.IsNullOrWhiteSpace(model))
            {
                chunk[ModelPropertyName] = model;
            }

            return SseDataPrefix + Space + chunk.ToJsonString(JsonRequestOptions) + NewLine + NewLine
                + SseDataPrefix + Space + SseDone + NewLine + NewLine;
        }
    }
}
