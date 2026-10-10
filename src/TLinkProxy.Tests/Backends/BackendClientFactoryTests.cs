using Microsoft.Extensions.DependencyInjection;
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
    public sealed class BackendClientFactoryTests
    {
        [TestMethod]
        public void CreateFor_OpenAIKind_ReturnsOpenAIBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.OpenAI });

            Assert.IsInstanceOfType<OpenAIBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_OpenAIKindMixedCase_ReturnsOpenAIBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.OpenAI });

            Assert.IsInstanceOfType<OpenAIBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_LlamaCppKind_ReturnsLlamaCppBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.LlamaCpp });

            Assert.IsInstanceOfType<LlamaCppBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_LlamaCppKindMixedCase_ReturnsLlamaCppBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.LlamaCpp });

            Assert.IsInstanceOfType<LlamaCppBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_OllamaKind_ReturnsOllamaBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.Ollama });

            Assert.IsInstanceOfType<OllamaBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_OllamaKindMixedCase_ReturnsOllamaBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.Ollama });

            Assert.IsInstanceOfType<OllamaBackendClient>(result);
        }


        [TestMethod]
        public void CreateFor_EmptyKind_ReturnsOllamaBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.None });

            Assert.IsInstanceOfType<OllamaBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_UnknownKind_ReturnsOllamaBackendClient()
        {
            BackendClientFactory sut = CreateFactory();

            IBackendClient result = sut.CreateFor(new AiModelConfig { Kind = ModelKind.None });

            Assert.IsInstanceOfType<OllamaBackendClient>(result);
        }

        [TestMethod]
        public void CreateFor_NullBackend_ThrowsArgumentNullException()
        {
            BackendClientFactory sut = CreateFactory();

            ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => sut.CreateFor(null));

            Assert.AreEqual("modelConfig", exception.ParamName);
        }

        [TestMethod]
        public void CreateFor_RepeatedCalls_ReturnsDistinctInstances()
        {
            BackendClientFactory sut = CreateFactory();
            AiModelConfig backend = new() { Kind = ModelKind.Ollama };

            IBackendClient first = sut.CreateFor(backend);
            IBackendClient second = sut.CreateFor(backend);

            Assert.AreNotSame(first, second);
        }

        private static BackendClientFactory CreateFactory()
        {
            ServiceCollection services = new();
            services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory());
            services.AddSingleton<IProxySessionLogger>(new FakeProxySessionLogger());
            return new BackendClientFactory(services.BuildServiceProvider());
        }

        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient(new StubHandler());
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        private sealed class FakeProxySessionLogger : IProxySessionLogger
        {
            public Task LogAsync(string sessionId, string message)
            {
                return Task.CompletedTask;
            }

            public Task LogBytesAsync(string sessionId, byte[] bytes, string label = null)
            {
                return Task.CompletedTask;
            }
        }
    }
}
