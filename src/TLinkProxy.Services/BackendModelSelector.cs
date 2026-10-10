using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Kernel;

namespace TLinkProxy.Services
{
    public sealed class BackendModelSelector : IBackendModelSelector
    {
        public string ResolveModel(IAiModelConfig selectedBackend)
        {
            if (selectedBackend == null)
                return null;

            string backendModel = selectedBackend.Model;

            if (String.IsNullOrWhiteSpace(backendModel))
                backendModel = selectedBackend.Alias;

            return String.IsNullOrWhiteSpace(backendModel) ? null : backendModel;
        }

        public string ApplyModel(OpenAIModel model, IAiModelConfig selectedBackend)
        {
            if (model == null || selectedBackend == null)
                return null;

            string backendModel = ResolveModel(selectedBackend);

            if (String.IsNullOrWhiteSpace(backendModel))
                return null;

            if (String.Equals(model.Model, backendModel, StringComparison.Ordinal))
                return null;

            model.Model = backendModel;
            return String.Format(Constants.OptimizerModelSetFormat, backendModel);
        }
    }
}
