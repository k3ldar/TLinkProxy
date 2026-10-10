using Microsoft.Extensions.Primitives;
using TLinkProxy.Abstractions;
using TLinkProxy.Kernel;

#pragma warning disable CC0009, CC0010

namespace TLinkProxy.Proxy.Controllers
{
    internal sealed class ProxyContext : IDisposable
    {
        internal ProxyContext(HttpRequest request, HttpMethod httpMethod, string pathAndQuery, string baseUrl,
            string targetUrl, StringValues authHeader, IOptimizationResult optimizationResult,
            OpenAIModel requestModel, string requestBodyText, string selectedBackendAlias)
        {
            Request = request;
            HttpMethod = httpMethod;
            PathAndQuery = pathAndQuery;
            Path = pathAndQuery?.Split('?')[0]?.TrimEnd('/').ToLowerInvariant() ?? String.Empty;
            BaseUrl = baseUrl;
            TargetUrl = targetUrl;
            AuthHeader = authHeader;
            OptimizationResult = optimizationResult;
            RequestBodyText = requestBodyText;
            RequestModel = requestModel;
            ForwardRequest = new(httpMethod, targetUrl);
            SelectedBackendAlias = selectedBackendAlias;
        }

        public HttpRequest Request { get; }

        public HttpMethod HttpMethod { get; }

        public string PathAndQuery { get; }

        public string Path { get; }

        public string BaseUrl { get; }

        public string TargetUrl { get; set; }

        public StringValues AuthHeader { get; }

        public IOptimizationResult OptimizationResult { get; internal set; }

        public string RequestBodyText { get; internal set; }

        public OpenAIModel RequestModel { get; internal set; }

        public HttpRequestMessage ForwardRequest { get; }

        public string SelectedBackendAlias { get; }

        public IDisposable Slot { get; private set; }

        public IDisposable GlobalSlot { get; private set; }

        public bool IsValid { get; private set; }

        public IAiModelConfig SelectedModel { get; private set; }

        public void Dispose()
        {
            Slot?.Dispose();
            GlobalSlot?.Dispose();
        }

        internal void UpdateModel(IAiModelConfig selectedModel, bool isValid, IDisposable slot, IDisposable globalSlot)
        {
            IsValid = isValid;
            Slot = slot;
            GlobalSlot = globalSlot;
            SelectedModel = selectedModel;
        }
    }
}

#pragma warning restore CC0009, CC0010
