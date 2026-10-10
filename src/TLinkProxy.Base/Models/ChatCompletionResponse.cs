using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Models
{
    public sealed class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = [];
    }
}
