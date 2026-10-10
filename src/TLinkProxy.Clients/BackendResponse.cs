using TLinkProxy.Abstractions;
using System.Net;
using System.Net.Http.Headers;

namespace TLinkProxy.Clients
{
    /// <summary>
    /// Wraps a backend's raw <see cref="HttpResponseMessage"/> so the controller can re-stream
    /// SSE or buffered bodies uniformly, independent of the concrete HTTP client used.
    /// </summary>
    public sealed class BackendResponse : IBackendResponse
    {
        private readonly HttpResponseMessage _message;

        public BackendResponse(HttpResponseMessage message)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public HttpResponseMessage Message => _message;

        public HttpStatusCode StatusCode => _message.StatusCode;

        public HttpResponseHeaders Headers => _message.Headers;

        public HttpContent Content => _message.Content;

        public void Dispose() => _message.Dispose();
    }
}
