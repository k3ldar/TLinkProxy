using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Clients;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace TLinkProxy.Tests.Backends
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class OpenAIBackendClientTests
    {
        [TestMethod]
        public void ClientLabel_Always_ReturnsOpenAIBackendLabel()
        {
            TestableOpenAIBackendClient sut = CreateSut();

            Assert.AreEqual(Constants.OpenAIBackendLabel, sut.ClientLabelForTest);
        }

        [TestMethod]
        public void ConfigureRequest_ApiKeyEnvVarNull_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            AiModelConfig backend = new() { ApiKeyEnvVar = null };
            using HttpRequestMessage request = new();

            sut.ConfigureRequestForTest(backend, request);

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_ApiKeyEnvVarEmpty_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            AiModelConfig backend = new() { ApiKeyEnvVar = "" };
            using HttpRequestMessage request = new();

            sut.ConfigureRequestForTest(backend, request);

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_ApiKeyEnvVarWhitespace_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            AiModelConfig backend = new() { ApiKeyEnvVar = "   " };
            using HttpRequestMessage request = new();

            sut.ConfigureRequestForTest(backend, request);

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_EnvVarNotSetInEnvironment_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_UNSET_VAR";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();

            try
            {
                Environment.SetEnvironmentVariable(envVarName, null);
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_EnvVarEmptyInEnvironment_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_EMPTY_VAR";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();

            try
            {
                Environment.SetEnvironmentVariable(envVarName, "");
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_EnvVarWhitespaceInEnvironment_DoesNotSetAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_WHITESPACE_VAR";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();

            try
            {
                Environment.SetEnvironmentVariable(envVarName, "   ");
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNull(request.Headers.Authorization);
        }

        [TestMethod]
        public void ConfigureRequest_EnvVarWithValue_SetsBearerAuthorizationHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_KEY_VAR";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();

            try
            {
                Environment.SetEnvironmentVariable(envVarName, "sk-test-123");
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNotNull(request.Headers.Authorization);
            Assert.AreEqual(Constants.AuthenticationSchemeBearer, request.Headers.Authorization.Scheme);
            Assert.AreEqual("sk-test-123", request.Headers.Authorization.Parameter);
        }

        [TestMethod]
        public void ConfigureRequest_EnvVarWithValue_PreservesExactKeyValue()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_EXACT_VAR";
            const string key = "  sk-leading-and-trailing-whitespace  ";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();

            try
            {
                Environment.SetEnvironmentVariable(envVarName, key);
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNotNull(request.Headers.Authorization);
            Assert.AreEqual(key, request.Headers.Authorization.Parameter);
        }

        [TestMethod]
        public void ConfigureRequest_NullBackend_ThrowsNullReferenceException()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            using HttpRequestMessage request = new();

            Assert.ThrowsExactly<NullReferenceException>(() => sut.ConfigureRequestForTest(null, request));
        }

        [TestMethod]
        public void ConfigureRequest_ExistingAuthorizationHeader_IsOverwrittenWithBearerHeader()
        {
            TestableOpenAIBackendClient sut = CreateSut();
            const string envVarName = "OPENAI_BACKEND_TEST_OVERWRITE_VAR";
            AiModelConfig backend = new() { ApiKeyEnvVar = envVarName };
            using HttpRequestMessage request = new();
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "old-value");

            try
            {
                Environment.SetEnvironmentVariable(envVarName, "sk-new-value");
                sut.ConfigureRequestForTest(backend, request);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, null);
            }

            Assert.IsNotNull(request.Headers.Authorization);
            Assert.AreEqual(Constants.AuthenticationSchemeBearer, request.Headers.Authorization.Scheme);
            Assert.AreEqual("sk-new-value", request.Headers.Authorization.Parameter);
        }

        private static TestableOpenAIBackendClient CreateSut()
        {
            return new TestableOpenAIBackendClient(new FakeHttpClientFactory(), new FakeProxySessionLogger());
        }

        private sealed class TestableOpenAIBackendClient : OpenAIBackendClient
        {
            public TestableOpenAIBackendClient(IHttpClientFactory factory, IProxySessionLogger logger)
                : base(factory, logger)
            {
            }

            public string ClientLabelForTest => ClientLabel;

            public void ConfigureRequestForTest(AiModelConfig backend, HttpRequestMessage request)
            {
                ConfigureRequest(backend, request);
            }
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
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
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
