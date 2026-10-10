using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TLinkProxy.Abstractions;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Models;
using TLinkProxy.Clients;
using TLinkProxy.Proxy.Controllers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TLinkProxy.Tests.Controllers
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class OllamaControllerTests
    {
        private const string TestAlias = "test-alias";
        private const string TestEndpoint = "http://localhost:11434";
        private const string ChatCompletionResponse = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Hello there\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":5,\"completion_tokens\":2}}";
        private const string EmbeddingResponse = "{\"object\":\"list\",\"data\":[{\"object\":\"embedding\",\"embedding\":[0.1,0.2,0.3],\"index\":0}]}";

        [TestMethod]
        public void Constructor_NullBackendRegistry_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OllamaController(null, CreateClientFactory(new CapturingHandler()),
                Substitute.For<ILlamaCppProcessManager>(), Substitute.For<IProxySessionLogger>(), Substitute.For<ITokenStatsStore>()));
        }

        [TestMethod]
        public void Constructor_NullBackendClientFactory_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OllamaController(Substitute.For<IAiBackendRegistry>(), null,
                Substitute.For<ILlamaCppProcessManager>(), Substitute.For<IProxySessionLogger>(), Substitute.For<ITokenStatsStore>()));
        }

        [TestMethod]
        public void Constructor_NullLlamaCppProcessManager_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OllamaController(Substitute.For<IAiBackendRegistry>(),
                CreateClientFactory(new CapturingHandler()), null, Substitute.For<IProxySessionLogger>(), Substitute.For<ITokenStatsStore>()));
        }

        [TestMethod]
        public void Constructor_NullSessionLogger_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OllamaController(Substitute.For<IAiBackendRegistry>(),
                CreateClientFactory(new CapturingHandler()), Substitute.For<ILlamaCppProcessManager>(), null, Substitute.For<ITokenStatsStore>()));
        }

        [TestMethod]
        public void Constructor_NullTokenStatsStore_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OllamaController(Substitute.For<IAiBackendRegistry>(),
                CreateClientFactory(new CapturingHandler()), Substitute.For<ILlamaCppProcessManager>(), Substitute.For<IProxySessionLogger>(), null));
        }

        [TestMethod]
        public void GetVersion_Always_ReturnsOllamaVersion()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out _);

            IActionResult result = sut.GetVersion();

            OkObjectResult okResult = (OkObjectResult)result;
            string json = JsonSerializer.Serialize(okResult.Value);
            Assert.AreEqual("{\"version\":\"0.6.5\"}", json);
        }

        [TestMethod]
        public void GetTags_WithBackends_ReturnsModelsWrapper()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out _);

            IActionResult result = sut.GetTags();

            OkObjectResult okResult = (OkObjectResult)result;
            TagsModel tags = (TagsModel)okResult.Value;
            Assert.HasCount(1, tags.models);
            Assert.AreEqual(TestAlias, tags.models[0].name);
        }

        [TestMethod]
        public void GetTags_NoBackends_ReturnsEmptyModelsArray()
        {
            OllamaController sut = CreateSut(CreateRegistry(), new CapturingHandler(), out _);

            IActionResult result = sut.GetTags();

            OkObjectResult okResult = (OkObjectResult)result;
            TagsModel tags = (TagsModel)okResult.Value;
            Assert.IsEmpty(tags.models);
        }

        [TestMethod]
        public async Task Show_KnownModel_ReturnsOkWithDetails()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\"}");

            IActionResult result = await sut.Show();

            OkObjectResult okResult = (OkObjectResult)result;
            string json = JsonSerializer.Serialize(okResult.Value);
            Assert.Contains("\"format\":\"gguf\"", json);
        }

        [TestMethod]
        public async Task Show_UnknownModel_ReturnsNotFound()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"missing-model\"}");

            await sut.Show();

            Assert.AreEqual((int)HttpStatusCode.NotFound, context.Response.StatusCode);
            string body = ReadResponseBody(context);
            Assert.Contains("missing-model", body);
        }

        [TestMethod]
        public void GetPs_OnDemandBackendNotRunning_ReturnsEmptyModels()
        {
            AiModelConfig backend = CreateBackend();
            backend.Details = new LlamaCppConfig { TimeoutSeconds = 30 };
            OllamaController sut = CreateSut(CreateRegistry(backend), new CapturingHandler(), out _);

            IActionResult result = sut.GetPs();

            OkObjectResult okResult = (OkObjectResult)result;
            string json = JsonSerializer.Serialize(okResult.Value);
            Assert.AreEqual("{\"models\":[]}", json);
        }

        [TestMethod]
        public void GetPs_BackendWithActivity_ReturnsModelEntry()
        {
            AiModelConfig backend = CreateBackend();
            ILlamaCppProcessManager processManager = Substitute.For<ILlamaCppProcessManager>();
            processManager.GetLastActivityUtc(TestAlias).Returns(DateTime.UtcNow);
            OllamaController sut = CreateSut(CreateRegistry(backend), new CapturingHandler(), processManager, out _);

            IActionResult result = sut.GetPs();

            OkObjectResult okResult = (OkObjectResult)result;
            string json = JsonSerializer.Serialize(okResult.Value);
            Assert.Contains(TestAlias, json);
        }

        [TestMethod]
        public void Delete_Always_ReturnsOk()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out _);

            IActionResult result = sut.Delete();

            Assert.IsInstanceOfType<OkResult>(result);
        }

        [TestMethod]
        public async Task Pull_NonStreamRequest_ReturnsSuccessStatus()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"stream\":false}");

            await sut.Pull();

            string body = ReadResponseBody(context);
            Assert.AreEqual("{\"status\":\"success\"}", body);
        }

        [TestMethod]
        public async Task Pull_StreamRequest_ReturnsNdjsonWithSuccess()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\"}");

            await sut.Pull();

            Assert.AreEqual("application/x-ndjson", context.Response.ContentType);
            string body = ReadResponseBody(context);
            Assert.Contains("pulling manifest", body);
            Assert.Contains("success", body);
        }

        [TestMethod]
        public async Task Push_NonStreamRequest_ReturnsSuccessStatus()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"stream\":false}");

            await sut.Push();

            string body = ReadResponseBody(context);
            Assert.AreEqual("{\"status\":\"success\"}", body);
        }

        [TestMethod]
        public async Task Create_NonStreamRequest_ReturnsSuccessStatus()
        {
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"stream\":false}");

            await sut.Create();

            string body = ReadResponseBody(context);
            Assert.AreEqual("{\"status\":\"success\"}", body);
        }

        [TestMethod]
        public async Task Chat_ValidRequest_ReturnsOllamaChatResponse()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(ChatCompletionResponse) };
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}");

            await sut.Chat();

            string body = ReadResponseBody(context);
            using JsonDocument document = JsonDocument.Parse(body);
            Assert.AreEqual("Hello there", document.RootElement.GetProperty("message").GetProperty("content").GetString());
            Assert.IsTrue(document.RootElement.GetProperty("done").GetBoolean());
            Assert.AreEqual(2, document.RootElement.GetProperty("eval_count").GetInt64());
        }

        [TestMethod]
        public async Task Chat_StreamRequest_ReturnsNdjsonChunks()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(ChatCompletionResponse) };
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.Chat();

            Assert.AreEqual("application/x-ndjson", context.Response.ContentType);
            string body = ReadResponseBody(context);
            string[] lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.HasCount(2, lines);
            using JsonDocument first = JsonDocument.Parse(lines[0]);
            using JsonDocument second = JsonDocument.Parse(lines[1]);
            Assert.IsFalse(first.RootElement.GetProperty("done").GetBoolean());
            Assert.IsTrue(second.RootElement.GetProperty("done").GetBoolean());
        }

        [TestMethod]
        public async Task Chat_NoBackendsConfigured_ReturnsNotFound()
        {
            OllamaController sut = CreateSut(CreateRegistry(), new CapturingHandler(), out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}");

            await sut.Chat();

            Assert.AreEqual((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task Generate_ValidRequest_ReturnsOllamaGenerateResponse()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(ChatCompletionResponse) };
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"prompt\":\"hi\",\"stream\":false}");

            await sut.Generate();

            string body = ReadResponseBody(context);
            using JsonDocument document = JsonDocument.Parse(body);
            Assert.AreEqual("Hello there", document.RootElement.GetProperty("response").GetString());
            Assert.IsTrue(document.RootElement.GetProperty("done").GetBoolean());
            Assert.AreEqual(5, document.RootElement.GetProperty("prompt_eval_count").GetInt64());
        }

        [TestMethod]
        public async Task Embeddings_ValidRequest_ReturnsEmbeddingVector()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(EmbeddingResponse) };
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"prompt\":\"hello\"}");

            await sut.Embeddings();

            string body = ReadResponseBody(context);
            using JsonDocument document = JsonDocument.Parse(body);
            Assert.AreEqual(3, document.RootElement.GetProperty("embedding").GetArrayLength());
        }

        [TestMethod]
        public async Task Chat_ValidRequest_RecordsTokenStats()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(ChatCompletionResponse) };
            ITokenStatsStore tokenStatsStore = Substitute.For<ITokenStatsStore>();
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, tokenStatsStore, out DefaultHttpContext context);
            string requestJson = "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}";
            SetRequestBody(context, requestJson);

            await sut.Chat();

            await tokenStatsStore.Received(1).RecordAsync(Arg.Any<string>(), requestJson.Length, Arg.Any<int>(), "test-model");
        }

        [TestMethod]
        public async Task Generate_ValidRequest_RecordsTokenStats()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(ChatCompletionResponse) };
            ITokenStatsStore tokenStatsStore = Substitute.For<ITokenStatsStore>();
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, tokenStatsStore, out DefaultHttpContext context);
            string requestJson = "{\"model\":\"" + TestAlias + "\",\"prompt\":\"hi\",\"stream\":false}";
            SetRequestBody(context, requestJson);

            await sut.Generate();

            await tokenStatsStore.Received(1).RecordAsync(Arg.Any<string>(), requestJson.Length, Arg.Any<int>(), "test-model");
        }

        [TestMethod]
        public async Task Embeddings_ValidRequest_RecordsTokenStats()
        {
            CapturingHandler handler = new() { Response = CreateJsonResponse(EmbeddingResponse) };
            ITokenStatsStore tokenStatsStore = Substitute.For<ITokenStatsStore>();
            OllamaController sut = CreateSut(CreateRegistry(CreateBackend()), handler, tokenStatsStore, out DefaultHttpContext context);
            string requestJson = "{\"model\":\"" + TestAlias + "\",\"prompt\":\"hello\"}";
            SetRequestBody(context, requestJson);

            await sut.Embeddings();

            await tokenStatsStore.Received(1).RecordAsync(Arg.Any<string>(), requestJson.Length, Arg.Any<int>(), "test-model");
        }

        [TestMethod]
        public async Task Chat_NoBackendsConfigured_DoesNotRecordTokenStats()
        {
            ITokenStatsStore tokenStatsStore = Substitute.For<ITokenStatsStore>();
            OllamaController sut = CreateSut(CreateRegistry(), new CapturingHandler(), tokenStatsStore, out DefaultHttpContext context);
            SetRequestBody(context, "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"stream\":false}");

            await sut.Chat();

            await tokenStatsStore.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        }

        private static OllamaController CreateSut(IAiBackendRegistry registry, CapturingHandler handler, out DefaultHttpContext context)
        {
            return CreateSut(registry, handler, Substitute.For<ILlamaCppProcessManager>(), out context);
        }

        private static OllamaController CreateSut(IAiBackendRegistry registry, CapturingHandler handler,
            ILlamaCppProcessManager processManager, out DefaultHttpContext context)
        {
            return CreateSut(registry, handler, processManager, Substitute.For<ITokenStatsStore>(), out context);
        }

        private static OllamaController CreateSut(IAiBackendRegistry registry, CapturingHandler handler,
            ITokenStatsStore tokenStatsStore, out DefaultHttpContext context)
        {
            return CreateSut(registry, handler, Substitute.For<ILlamaCppProcessManager>(), tokenStatsStore, out context);
        }

        private static OllamaController CreateSut(IAiBackendRegistry registry, CapturingHandler handler,
            ILlamaCppProcessManager processManager, ITokenStatsStore tokenStatsStore, out DefaultHttpContext context)
        {
            BackendClientFactory clientFactory = CreateClientFactory(handler);
            OllamaController sut = new(registry, clientFactory, processManager, Substitute.For<IProxySessionLogger>(), tokenStatsStore);

            context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            sut.ControllerContext = new ControllerContext { HttpContext = context };

            return sut;
        }

        private static BackendClientFactory CreateClientFactory(CapturingHandler handler)
        {
            ServiceCollection services = new();
            services.AddSingleton<IHttpClientFactory>(new CapturingHttpClientFactory(handler));
            services.AddSingleton<IProxySessionLogger>(Substitute.For<IProxySessionLogger>());
            ServiceProvider provider = services.BuildServiceProvider();
            return new BackendClientFactory(provider);
        }

        private static IAiBackendRegistry CreateRegistry(params IAiModelConfig[] backends)
        {
            IAiBackendRegistry registry = Substitute.For<IAiBackendRegistry>();
            registry.GetAllBackends().Returns(backends);
            return registry;
        }

        private static AiModelConfig CreateBackend()
        {
            return new AiModelConfig
            {
                Alias = TestAlias,
                Model = "test-model",
                Endpoint = TestEndpoint,
                Kind = ModelKind.Ollama,
                Enabled = true
            };
        }

        private static HttpResponseMessage CreateJsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static void SetRequestBody(DefaultHttpContext context, string json)
        {
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            context.Request.ContentType = "application/json";
        }

        private static string ReadResponseBody(DefaultHttpContext context)
        {
            context.Response.Body.Position = 0;
            using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            return reader.ReadToEnd();
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
    }
}
