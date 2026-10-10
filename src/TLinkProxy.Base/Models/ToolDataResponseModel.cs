namespace TLinkProxy.Base.Models
{
    public sealed class ToolDataResponseModel
    {
        public string model { get; set; }
        public string license { get; set; }
        public string modelfile { get; set; }
        public string parameters { get; set; }
        public string template { get; set; }
        public Details details { get; set; }
        public Model_Info model_info { get; set; }
        public Tensor[] tensors { get; set; }
        public string[] capabilities { get; set; }
        public string modified_at { get; set; }
    }
}
