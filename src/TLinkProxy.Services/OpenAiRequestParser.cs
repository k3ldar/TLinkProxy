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

                if (doc is JsonObject root)
                {
                    RoutingEnvelope routing = null;

                    // Top-level routing envelope
                    if (root.TryGetPropertyValue(Constants.RoutingKey, out JsonNode topLevelRouting))
                    {
                        try
                        {
                            routing = JsonSerializer.Deserialize<RoutingEnvelope>(topLevelRouting.ToJsonString(), Constants.JsonRequestOptions);
                        }
                        catch
                        {
                            routing = null;
                        }

                        root.Remove(Constants.RoutingKey);
                    }

                    // Fallback: routing encoded as a reserved system message
                    if (root.TryGetPropertyValue(Constants.MessagesKey, out JsonNode messagesNode) && messagesNode is JsonArray messages)
                    {
                        const string marker = Constants.RoutingSystemMessageMarker;
                        for (int i = 0; i < messages.Count; i++)
                        {
                            if (messages[i] is JsonObject msgObj)
                            {
                                string role = msgObj[Constants.MessageTypeRole]?.GetValue<string>() ?? String.Empty;
                                string content = msgObj[Constants.MessageConstant]?.GetValue<string>() ?? String.Empty;

                                if (String.Equals(role, Constants.RoleTypeSystem, StringComparison.OrdinalIgnoreCase) && content.StartsWith(marker, StringComparison.Ordinal))
                                {
                                    if (routing == null)
                                    {
                                        string routingJson = content[marker.Length..];
                                        try
                                        {
                                            routing = JsonSerializer.Deserialize<RoutingEnvelope>(routingJson, Constants.JsonRequestOptions);
                                        }
                                        catch
                                        {
                                            routing = null;
                                        }
                                    }

                                    // remove the routing system message so it is not forwarded downstream
                                    messages.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }

                    string modified = doc.ToJsonString();
                    try
                    {
                        var model = JsonSerializer.Deserialize<OpenAIModel>(modified, Constants.JsonRequestOptions);
                        return (model, routing, normalized);
                    }
                    catch
                    {
                        // If deserialization of modified JSON fails, attempt to deserialize original normalized body
                        try
                        {
                            var fallback = JsonSerializer.Deserialize<OpenAIModel>(normalized, Constants.JsonRequestOptions);
                            return (fallback, routing, normalized);
                        }
                        catch
                        {
                            return (null, routing, normalized);
                        }
                    }
                }

                // If not a JsonObject root, attempt direct deserialization
                var direct = JsonSerializer.Deserialize<OpenAIModel>(normalized, Constants.JsonRequestOptions);
                return (direct, null, normalized);
            }
            catch
            {
                // Malformed JSON
                return (null, null, normalized);
            }
        }
    }
}
