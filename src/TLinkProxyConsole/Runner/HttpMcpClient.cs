using TLinkProxy.Base;
using TLinkProxyConsole.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TLinkProxyConsole.Runner
{
    internal sealed class HttpMcpClient : IDisposable
    {
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = false,
        };

        private readonly HttpClient _http;

        internal HttpMcpClient()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(Constants.JsonContentType));
        }

        /// <summary>
        /// Sends <paramref name="fixture"/>'s request to the proxy endpoint and
        /// returns the raw response body string.
        /// </summary>
        internal async Task<string> SendAsync(
            TestFixture fixture,
            CancellationToken cancellationToken = default)
        {
            string url = BuildUrl(fixture);

            using HttpRequestMessage request = new(ParseMethod(fixture.Method), url);

            if (fixture.Request.ValueKind != JsonValueKind.Undefined)
            {
                string requestJson = JsonSerializer.Serialize(fixture.Request, _serializeOptions);
                request.Content = new StringContent(requestJson, Encoding.UTF8, Constants.JsonContentType);
            }

            HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            string body = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Server returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");

            return body;
        }

        private static string BuildUrl(TestFixture fixture)
        {
            if (!String.IsNullOrWhiteSpace(fixture.Url))
                return fixture.Url.TrimEnd('/');

            string path = fixture.Path?.TrimStart('/');

            return String.IsNullOrWhiteSpace(path)
                ? ConsoleConstants.DefaultServerUrl.TrimEnd('/')
                : $"{ConsoleConstants.DefaultServerUrl.TrimEnd('/')}/{path}";
        }

        private static HttpMethod ParseMethod(string method)
        {
            return (method ?? "POST").Trim().ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "PATCH" => HttpMethod.Patch,
                "DELETE" => HttpMethod.Delete,
                _ => new HttpMethod(method.Trim())
            };
        }

        public void Dispose() => _http.Dispose();
    }
}
