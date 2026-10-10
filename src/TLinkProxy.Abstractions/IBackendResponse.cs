using System.Net;
using System.Net.Http.Headers;

namespace TLinkProxy.Abstractions
{
    public interface IBackendResponse : IDisposable
    {
        HttpContent Content { get; }
        HttpResponseHeaders Headers { get; }
        HttpResponseMessage Message { get; }
        HttpStatusCode StatusCode { get; }
    }
}