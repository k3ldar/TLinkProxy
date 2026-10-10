namespace TLinkProxy.Base.Models
{
    public sealed class Tensor
    {
        public string name { get; set; }
        public string type { get; set; }
        public int[] shape { get; set; }
    }
}
