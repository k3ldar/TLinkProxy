using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxyConsole.Models
{
    /// <summary>
    /// Represents a single integration test fixture loaded from a JSON file.
    /// </summary>
    internal sealed class TestFixture
    {
        /// <summary>
        /// Human-readable name displayed in the runner output.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = String.Empty;

        /// <summary>
        /// Optional full base URL for the proxy.  When omitted the runner uses
        /// <see cref="ConsoleConstants.DefaultServerUrl"/>.  When supplied it is used
        /// as-is (the <see cref="Path"/> value is ignored).
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// HTTP method to use.  Defaults to <c>POST</c>.
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = "POST";

        /// <summary>
        /// Endpoint path appended to the base URL, e.g. <c>v1/chat/completions</c>.
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>
        /// When <c>true</c> the response is only checked for a non-empty
        /// conversational reply rather than compared against
        /// <see cref="ExpectedResponse"/>.  Use for chat/completion endpoints.
        /// </summary>
        [JsonPropertyName("conversation")]
        public bool Conversation { get; set; }

        /// <summary>
        /// When <c>true</c> the response is only checked to be a non-empty JSON
        /// array.  Use for list endpoints such as <c>v1/tags</c>.
        /// </summary>
        [JsonPropertyName("nonEmptyArray")]
        public bool NonEmptyArray { get; set; }

        /// <summary>
        /// When <c>true</c> (the default) the entire actual response JSON must equal
        /// the expected response JSON.  When <c>false</c> only the fields present in
        /// <see cref="ExpectedResponse"/> are asserted; extra fields in the actual
        /// response are ignored.
        /// </summary>
        [JsonPropertyName("strictMatch")]
        public bool StrictMatch { get; set; } = true;

        /// <summary>
        /// JSON request body to send to the proxy endpoint.
        /// </summary>
        [JsonPropertyName("request")]
        public JsonElement Request { get; set; }

        /// <summary>
        /// The response the runner expects to receive from the server.
        /// </summary>
        [JsonPropertyName("expectedResponse")]
        public JsonElement ExpectedResponse { get; set; }
    }
}
