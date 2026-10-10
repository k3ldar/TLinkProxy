using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Models
{
    public sealed class TagDetails
    {
        public string parent_model { get; set; }
        public string format { get; set; }
        public ModelKind family { get; set; }
        public ModelKind families { get; set; }
        public string parameter_size { get; set; }
        public string quantization_level { get; set; }
    }
}
