using TLinkProxy.Abstractions;
using TLinkProxy.Base.Configuration;

namespace TLinkProxy.Configuration.Models
{
    public sealed class AiModelEditViewModel
    {
        public AiModelEditViewModel()
        {
            Backend = new AiModelConfig
            {
                Details = new LlamaCppConfig()
            };
        }

        public IAiModelConfig Backend { get; set; }

        public string OriginalAlias { get; set; }

        public string TagsCsv { get; set; }

        public string[] InstalledModels { get; set; }
    }
}
