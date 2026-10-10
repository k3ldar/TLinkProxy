using TLinkProxy.Kernel;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Models
{
    public sealed class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }

        [JsonPropertyName("delta")]
        public Message Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }
}
