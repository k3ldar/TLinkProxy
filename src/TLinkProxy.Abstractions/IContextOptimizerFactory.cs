namespace TLinkProxy.Abstractions
{
    public interface IContextOptimizerFactory
    {
        List<IContextOptimizer> GetOptimizers(string requestBodyText);
    }
}
