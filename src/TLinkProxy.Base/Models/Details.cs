namespace TLinkProxy.Base.Models
{
    public sealed class Details
    {
        public string parent_model { get; set; }
        public string format { get; set; }
        public string family { get; set; }
        public string[] families { get; set; }
        public string parameter_size { get; set; }
        public string quantization_level { get; set; }

        public int context_length { get; set; }
    }
}
