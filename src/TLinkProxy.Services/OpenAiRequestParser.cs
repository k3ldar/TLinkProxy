using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Kernel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TLinkProxy.Services
{
    public sealed class OpenAiRequestParser : IOpenAiRequestParser
    {
        public async Task<(OpenAIModel Model, RoutingEnvelope Routing, string NormalizedBody)> ParseAsync(string body, string contentType)
        {
            // Keep method async-compatible for future logging or IO; currently synchronous work only.
            await Task.CompletedTask;

            if (String.IsNullOrWhiteSpace(body))
                return (null, null, body);

            if (String.IsNullOrWhiteSpace(contentType) || !contentType.Contains(Constants.JsonContentTypeToken, StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, body);
            }

            // Normalize CRLF -> LF
            string normalized = body.Replace(Constants.CrlfNewLine, Constants.NewLine);

            try
            {
                JsonNode doc = JsonNode.Parse(normalized);

                if (doc is not JsonObject root)
                {
                    // Non-object JSON root: attempt direct deserialization.
                    return (DeserializeModel(normalized), null, normalized);
                }

                // Routing envelope is either a top-level routing object or a reserved system message.
                RoutingEnvelope routing = ExtractTopLevelRouting(root);

                // Single deserialization pass; Message.Content's converter flattens array content blocks.
                OpenAIModel model = DeserializeModel(normalized);

                if (model == null)
                {
                    // Full deserialization failed; preserve the routing-critical model name for backend selection.
                    model = CreateFallbackModel(root);
                }
                else
                {
                    // Remove any reserved routing system message so it is not forwarded downstream.
                    RemoveRoutingMessage(model, ref routing);
                }

                return (model, routing, normalized);
            }
            catch
            {
                // Malformed JSON
                return (null, null, normalized);
            }
        }

        private static RoutingEnvelope ExtractTopLevelRouting(JsonObject root)
        {
            if (!root.TryGetPropertyValue(Constants.RoutingKey, out JsonNode topLevelRouting))
            {
                return null;
            }

            return DeserializeRouting(topLevelRouting?.ToJsonString());
        }

        private static void RemoveRoutingMessage(OpenAIModel model, ref RoutingEnvelope routing)
        {
            if (model?.Messages == null)
            {
                return;
            }

            const string marker = Constants.RoutingSystemMessageMarker;

            for (int i = 0; i < model.Messages.Length; i++)
            {
                Message message = model.Messages[i];

                if (message == null)
                    continue;

                if (!String.Equals(message.Role, Constants.RoleTypeSystem, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (String.IsNullOrEmpty(message.Content) || !message.Content.StartsWith(marker, StringComparison.Ordinal))
                    continue;

                // Parse the embedded envelope only when no top-level envelope was supplied.
                if (routing == null)
                {
                    routing = DeserializeRouting(message.Content[marker.Length..]);
                }

                var remaining = new List<Message>(model.Messages);
                remaining.RemoveAt(i);
                model.Messages = [.. remaining];
                break;
            }
        }

        private static OpenAIModel DeserializeModel(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<OpenAIModel>(json, Constants.JsonRequestOptions);
            }
            catch
            {
                return null;
            }
        }

        private static RoutingEnvelope DeserializeRouting(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);
            }
            catch
            {
                return null;
            }
        }

        private static OpenAIModel CreateFallbackModel(JsonObject root)
        {
            string model = null;

            if (root.TryGetPropertyValue(Constants.ModelPropertyName, out JsonNode modelNode) && modelNode is JsonValue modelValue)
            {
                model = modelValue.TryGetValue<string>(out string text) ? text : modelValue.ToString();
            }

            return new OpenAIModel { Model = model, Messages = null };
        }
    }
}
