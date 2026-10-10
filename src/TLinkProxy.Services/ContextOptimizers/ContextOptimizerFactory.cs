using TLinkProxy.Abstractions;

namespace TLinkProxy.Services.ContextOptimizers
{
    internal class ContextOptimizerFactory : IContextOptimizerFactory
    {
        private readonly List<IContextOptimizer> _optimizers;

        public ContextOptimizerFactory(IProxySessionLogger sessionLogger)
        {
            _optimizers = [new VisualStudioContextOptimizer(sessionLogger)];
        }

        public List<IContextOptimizer> GetOptimizers(string requestBodyText)
        {
            List<IContextOptimizer> optimizers = [];

            foreach (IContextOptimizer optimizer in _optimizers)
            {
                if (optimizer.CanOptimize(requestBodyText))
                {
                    optimizers.Add(optimizer);
                }
            }

            return optimizers;
        }
    }
}
