using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Kernel;

namespace TLinkProxy.Services.ContextOptimizers
{
    public sealed class VisualStudioContextOptimizer : IContextOptimizer
    {
        private readonly IProxySessionLogger _sessionLogger;
        private const int MaxMessageLength = 65536;
        private const int DefaultMaxCompletionTokens = 8196;
        private const int MaxAllowedCompletionTokens = 65536;

        public VisualStudioContextOptimizer(IProxySessionLogger sessionLogger)
        {
            _sessionLogger = sessionLogger ?? throw new ArgumentNullException(nameof(sessionLogger));
        }

        public bool CanOptimize(string requestBody)
        {
            if (String.IsNullOrWhiteSpace(requestBody))
            {
                return false;
            }

            return requestBody.Contains("are the GitHub Copilot") &&
                requestBody.Contains("<version_information>Version number:");
        }

        public IReadOnlyList<string> Optimize(OpenAIModel model, string sessionId)
        {
            ArgumentNullException.ThrowIfNull(model);

            var changes = new List<string>();

            OptimizeMessages(model, changes);
            NormalizeCustomToolCalls(model, changes);
            NormalizeCompletionTokens(model, changes);
            OptimizeTools(model, changes);
            RemoveEmptyToolChoice(model, changes);
            EnsureSessionMarker(model, sessionId, changes);

            return changes;
        }

        private static void OptimizeMessages(OpenAIModel model, List<string> changes)
        {
            if (model?.Messages == null || model.Messages.Length == 0)
                return;

            var messages = new List<Message>();
            string previousMessage = String.Empty;
            int i = 0;

            foreach (var msg in model.Messages)
            {
                if (msg == null)
                {
                    i++;
                    continue;
                }

                // remove single-word user messages (resume, continue etc)
                if (String.Equals(msg.Role, Constants.UserRole, StringComparison.OrdinalIgnoreCase)
                    && !String.IsNullOrEmpty(msg.Content)
                    && msg.Content.Length < Constants.ShortMessageLengthThreshold
                    && !msg.Content.Contains(Constants.Space))
                {
                    i++;
                    continue;
                }

                if (msg.Content != null)
                {
                    msg.Content = msg.Content.Replace(Constants.CrlfNewLine, Constants.NewLine);

                    if (!String.Equals(msg.Role, Constants.ToolRole, StringComparison.OrdinalIgnoreCase)
                        && msg.Content.Equals(previousMessage))
                    {
                        i++;
                        continue;
                    }

                    if (msg.Content.Length > MaxMessageLength)
                    {
                        msg.Content = String.Concat(msg.Content.AsSpan(0, MaxMessageLength), Constants.TruncatedSuffix);
                        changes.Add(String.Format(Constants.OptimizerMessageTruncatedFormat, i));
                    }
                }

                messages.Add(msg);
                previousMessage = msg.Content ?? String.Empty;
                i++;
            }

            model.Messages = [.. messages];
        }

        private static void NormalizeCustomToolCalls(OpenAIModel model, List<string> changes)
        {
            if (model?.Messages == null)
                return;

            bool changed = false;

            foreach (var message in model.Messages)
            {
                if (message?.ToolCalls == null)
                    continue;

                foreach (var call in message.ToolCalls)
                {
                    if (call?.Custom == null)
                        continue;

                    call.Function = new FunctionCall
                    {
                        Name = call.Custom.Name,
                        Arguments = call.Custom.Input
                    };
                    call.Custom = null;
                    call.Type = Constants.ToolCallTypeFunction;
                    changed = true;
                }
            }

            if (changed)
                changes.Add(Constants.CustomToolCallNormalized);
        }

        private static void NormalizeCompletionTokens(OpenAIModel model, List<string> changes)
        {
            if (model == null)
                return;

            if (model.MaxCompletionTokens <= 0 || model.MaxCompletionTokens > MaxAllowedCompletionTokens)
            {
                model.MaxCompletionTokens = DefaultMaxCompletionTokens;
                changes.Add(Constants.MaxCompletionTokensNormalized);
            }
        }

        private static void OptimizeTools(OpenAIModel model, List<string> changes)
        {
            if (model?.Tools == null || model.Tools.Length == 0)
                return;

            var kept = new List<Tool>();

            foreach (var tool in model.Tools)
            {
                if (tool == null || tool.Function == null || String.IsNullOrWhiteSpace(tool.Function.Name))
                    continue;

                // Remove tools that reference Azure (conservative)
                if (tool.Function.Name.Contains(Constants.AzureKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(String.Format(Constants.OptimizerRemovedToolFormat, tool.Function.Name));
                    continue;
                }

                if (tool.Function.Parameters != null && tool.Function.Parameters.AdditionalProperties == true)
                {
                    tool.Function.Parameters.AdditionalProperties = false;
                    changes.Add(String.Format(Constants.OptimizerToolConfigDisabledFormat, tool));
                }

                if (tool.Function.Description != null)
                {
                    tool.Function.Description = tool.Function.Description.Replace(Constants.CrlfNewLine, Constants.NewLine);
                }

                kept.Add(tool);
            }

            model.Tools = [.. kept];
        }

        private static void RemoveEmptyToolChoice(OpenAIModel model, List<string> changes)
        {
            if (model == null)
                return;

            if (model.ToolChoice != null && String.IsNullOrWhiteSpace(model.ToolChoice))
            {
                model.ToolChoice = null;
                changes.Add(Constants.ToolChoiceCleared);
            }
        }

        private static void EnsureSessionMarker(OpenAIModel model, string sessionId, List<string> changes)
        {
            if (model == null || String.IsNullOrWhiteSpace(sessionId))
                return;

            if ((model.Messages ?? []).Any(IsSessionMarker))
                return;

            var marker = new Message
            {
                Role = Constants.RoleTypeSystem,
                Content = String.Concat(Constants.SessionMarkerPrefix, sessionId, Constants.SessionMarkerSuffix)
            };

            var messages = new List<Message> { marker };
            if (model.Messages != null)
                messages.AddRange(model.Messages);

            model.Messages = [.. messages];
            changes.Add(Constants.SessionMarkerInjected);
        }

        private static bool IsSessionMarker(Message message)
        {
            return message != null
                && String.Equals(message.Role, Constants.RoleTypeSystem, StringComparison.OrdinalIgnoreCase)
                && message.Content != null
                && message.Content.StartsWith(Constants.SessionMarkerPrefix, StringComparison.Ordinal);
        }



















        //            string optimizedJson;
        //            try
        //            {
        //#pragma warning disable CC0009 // Avoid magic numbers (and un-named literals)
        //#pragma warning disable CC0010 // String literal duplicated

        //                if (File.Exists(Constants.GithubCopilotPromptPath))
        //                {
        //                    Message promptTarget = modelContext.Messages.FirstOrDefault(m => !IsSessionMarker(m));

        //                    if (promptTarget != null)
        //                        promptTarget.Content = File.ReadAllText(Constants.GithubCopilotPromptPath);
        //                }

        //#pragma warning restore CC0010 // String literal duplicated
        //#pragma warning restore CC0009 // Avoid magic numbers (and un-named literals)
        //                optimizedJson = JsonSerializer.Serialize(modelContext, Constants.JsonRequestOptions);
        //            }
        //            catch (Exception ex)
        //            {
        //                await _sessionLogger.LogAsync(parsed.SessionId, String.Format(Constants.OptimizerSerializeFailedFormat, ex.Message));
        //                return null;
        //            }
    }
}