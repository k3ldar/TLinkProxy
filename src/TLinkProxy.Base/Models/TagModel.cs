namespace TLinkProxy.Base.Models
{
    public sealed class TagModel
    {
        public string name { get; set; }
        public string model { get; set; }
        public string modified_at { get; set; }
        public long size { get; set; }
        public string digest { get; set; }
        public TagDetails details { get; set; }
    }
}
