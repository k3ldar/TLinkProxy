using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IContextOptimizer
    {
        /// <summary>
        /// Determines whether the context optimizer can run
        /// </summary>
        /// <param name="requestBodyText"></param>
        /// <returns></returns>
        bool CanOptimize(string requestBodyText);

        /// <summary>
        /// Optimize the provided OpenAIModel in-place and return a list of change tokens/descriptions.
        /// </summary>
        IReadOnlyList<string> Optimize(OpenAIModel model, string sessionId);
    }
}