using TLinkProxy.Abstractions;
using TLinkProxy.Kernel;
using System.Text.Json;

namespace TLinkProxy.Base.Models
{
    public sealed class ParsedRequest : IParsedRequest
    {
        public ParsedRequest(RoutingEnvelope routing, string body, string contentType, string sessionId)
        {
            ArgumentNullException.ThrowIfNull(routing);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(contentType);
            ArgumentNullException.ThrowIfNull(sessionId);
            Routing = routing;
            Body = body;
            ContentType = contentType;
            SessionId = ExtractSessionId(RequestModel) ?? sessionId;
        }

        public string Body { get; set; }

        public OpenAIModel RequestModel => JsonSerializer.Deserialize<OpenAIModel>(Body, Constants.JsonRequestOptions);

        public RoutingEnvelope Routing { get; }

        public List<string> Changes { get; } = [];

        public string ContentType { get; }

        public string SessionId { get; }

        private static string ExtractSessionId(OpenAIModel modelContext)
        {
            if (modelContext?.Messages == null)
            {
                return null;
            }

            foreach (Message message in modelContext.Messages)
            {
                string contentSessionId = ExtractSessionIdFromText(message?.Content);

                if (contentSessionId != null)
                {
                    return contentSessionId;
                }

                if (message?.ToolCalls == null)
                {
                    continue;
                }

                foreach (ToolCall call in message.ToolCalls)
                {
                    string toolSessionId = ExtractSessionIdFromText(call?.Arguments);

                    if (toolSessionId != null)
                    {
                        return toolSessionId;
                    }
                }
            }

            return null;
        }

        private static string ExtractSessionIdFromText(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            int prefixIndex = text.IndexOf(Constants.SessionMarkerPrefix, StringComparison.Ordinal);

            if (prefixIndex < 0)
            {
                return null;
            }

            int valueStart = prefixIndex + Constants.SessionMarkerPrefix.Length;
            int suffixIndex = text.IndexOf(Constants.SessionMarkerSuffix, valueStart, StringComparison.Ordinal);

            if (suffixIndex < 0)
            {
                return null;
            }

            string candidate = text[valueStart..suffixIndex].Trim();
            return Guid.TryParse(candidate, out _) ? candidate : null;
        }
    }
}
