using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Clients;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace TLinkProxy.Tests.Backends
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class LlamaCppBackendClientTests
    {
        [TestMethod]
        public void ClientLabel_Always_ReturnsLlamaCppBackendLabel()
        {
            TestableLlamaCppBackendClient sut = CreateSut();

            Assert.AreEqual(Constants.LlamaCppBackendLabel, sut.ClientLabelForTest);
        }

        [TestMethod]
        public async Task SendAsync_ValidRequest_DoesNotSetAuthorizationHeader()
        {
            (TestableLlamaCppBackendClient sut, CapturingHandler handler) = CreateSutWithHandler();
            AiModelConfig backend = new() { Endpoint = "http://localhost:11434" };

            await sut.SendAsync(backend, "/health", "{}", "s1", CancellationToken.None);

            Assert.IsNull(handler.CapturedRequest.Headers.Authorization);
        }

        [TestMethod]
        public async Task SendAsync_ValidRequest_LogsWithLlamaCppBackendLabel()
        {
            CapturingHttpClientFactory factory = new(new CapturingHandler());
            FakeProxySessionLogger logger = new();
            TestableLlamaCppBackendClient sut = new(factory, logger);
            AiModelConfig backend = new() { Endpoint = "http://localhost:11434" };

            await sut.SendAsync(backend, "/health", "{}", "s1", CancellationToken.None);

            Assert.HasCount(1, logger.Entries);
            Assert.AreEqual("[LlamaCppBackend] POST http://localhost:11434/health", logger.Entries[0].Message);
        }

        [TestMethod]
        public async Task SendAsync_ValidRequest_ReturnsBackendResponseWithStatusCode()
        {
            (TestableLlamaCppBackendClient sut, CapturingHandler handler) = CreateSutWithHandler();
            handler.Response = new HttpResponseMessage(HttpStatusCode.NoContent);
            AiModelConfig backend = new() { Endpoint = "http://localhost:11434" };

            IBackendResponse result = await sut.SendAsync(backend, "/health", "{}", "s1", CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        private static (TestableLlamaCppBackendClient Sut, CapturingHandler Handler) CreateSutWithHandler()
        {
            CapturingHandler handler = new();
            CapturingHttpClientFactory factory = new(handler);
            FakeProxySessionLogger logger = new();
            TestableLlamaCppBackendClient sut = new(factory, logger);
            return (sut, handler);
        }

        private static TestableLlamaCppBackendClient CreateSut()
        {
            return CreateSutWithHandler().Sut;
        }

        private sealed class TestableLlamaCppBackendClient : LlamaCppBackendClient
        {
            public TestableLlamaCppBackendClient(IHttpClientFactory factory, IProxySessionLogger logger)
                : base(factory, logger)
            {
            }

            public string ClientLabelForTest => ClientLabel;
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage CapturedRequest { get; private set; }

            public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedRequest = request;
                return Task.FromResult(Response);
            }
        }

        private sealed class CapturingHttpClientFactory : IHttpClientFactory
        {
            private readonly CapturingHandler _handler;

            public CapturingHttpClientFactory(CapturingHandler handler)
            {
                _handler = handler;
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient(_handler);
            }
        }

        private sealed class FakeProxySessionLogger : IProxySessionLogger
        {
            public List<LogEntry> Entries { get; } = [];

            public Task LogAsync(string sessionId, string message)
            {
                Entries.Add(new LogEntry(sessionId, message));
                return Task.CompletedTask;
            }

            public Task LogBytesAsync(string sessionId, byte[] bytes, string label = null)
            {
                return Task.CompletedTask;
            }
        }

        private sealed record LogEntry(string SessionId, string Message);
    }
}
