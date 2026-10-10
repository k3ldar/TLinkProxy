using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using System.Text;
using System.Text.Json;

namespace TLinkProxy.Clients
{
    /// <summary>
    /// Shared HTTP transport for backend adapters. Subclasses customize authentication and logging.
    /// </summary>
    public abstract class HttpBackendClientBase : IBackendClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly IProxySessionLogger _logger;

        protected HttpBackendClientBase(IHttpClientFactory factory, IProxySessionLogger logger)
        {
            _factory = factory;
            _logger = logger;
        }

        protected abstract string ClientLabel { get; }

        public virtual async Task<IBackendResponse> SendAsync(
            IAiModelConfig modelConfig,
            string pathAndQuery,
            string bodyJson,
            string sessionId,
            CancellationToken ct)
        {
            HttpClient client = _factory.CreateClient(Constants.BackendClientName);
            client.Timeout = TimeSpan.FromMinutes(Constants.DefaultHttpTimeoutMinutes);

            string baseUrl = modelConfig.Endpoint?.TrimEnd(Constants.CharForwardSlash) ?? "";
            string url = baseUrl + pathAndQuery;

            string outgoingBody = RewriteModel(bodyJson, modelConfig.Model);

            using HttpRequestMessage request = new(HttpMethod.Post, url)
            {
                Content = new StringContent(outgoingBody ?? Constants.EmptyString, Encoding.UTF8, Constants.JsonContentType),
            };

            ConfigureRequest(modelConfig, request);

            await _logger.LogAsync(sessionId, $"[{ClientLabel}] POST {url}").ConfigureAwait(false);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            return new BackendResponse(response);
        }

        /// <summary>Hook for subclasses to add authentication or other request headers.</summary>
        protected virtual void ConfigureRequest(IAiModelConfig modelConfig, HttpRequestMessage request)
        {
        }

        private static string RewriteModel(string bodyJson, string backendModel)
        {
            if (String.IsNullOrWhiteSpace(backendModel) || String.IsNullOrWhiteSpace(bodyJson))
            {
                return bodyJson;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(bodyJson);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return bodyJson;
                }

                using MemoryStream stream = new();
                using (Utf8JsonWriter writer = new(stream))
                {
                    writer.WriteStartObject();
                    bool modelWritten = false;

                    foreach (JsonProperty property in document.RootElement.EnumerateObject())
                    {
                        if (String.Equals(property.Name, Constants.ModelPropertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteString(Constants.ModelPropertyName, backendModel);
                            modelWritten = true;
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!modelWritten)
                    {
                        writer.WriteString(Constants.ModelPropertyName, backendModel);
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return bodyJson;
            }
        }
    }
}
