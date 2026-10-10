namespace TLinkProxy.Abstractions
{
    public interface IAiSettings
    {
        int GlobalQueueCapacity { get; set; }
        string Llamacpp { get; set; }
        string ModelPath { get; set; }
        bool LogToFile { get; set; }
    }
}