using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Models;
using TLinkProxy.Clients;
using TLinkProxy.Kernel;
using TLinkProxy.Proxy.Controllers;

namespace TLinkProxy.Tests.Controllers
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    [DoNotParallelize]
    public sealed class ProxyControllerTests
    {
        private const string TestAlias = "test-alias";
        private const string TestModel = "test-model";
        private const string TestEndpoint = "http://localhost:11434";
        private const string TestSessionId = "session-1";
        private const string ChatCompletionResponse = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Hello there\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":5,\"completion_tokens\":2}}";

        private string _previousRemoteOllamaUrl;
        private IProxySessionLogger _sessionLogger;
        private IContextOptimizerFactory _contextOptimizerFactory;
        private ISessionRegistry _sessionRegistry;
        private IToolCallInspector _toolCallInspector;
        private IAiBackendRegistry _backendRegistry;
        private IBackendRouter _backendRouter;
        private ILlamaCppProcessManager _llamaCppProcessManager;
        private IModelScheduler _modelScheduler;
        private IOpenAiRequestParser _openAiRequestParser;
        private IBackendModelSelector _backendModelSelector;
        private ITokenStatsStore _tokenStatsStore;
        private BackendClientFactory _backendClientFactory;
        private CapturingHandler _handler;

        [TestInitialize]
        public void SetUp()
        {
            _previousRemoteOllamaUrl = Environment.GetEnvironmentVariable(Constants.RemoteOllamaUrlEnvVar);
            Environment.SetEnvironmentVariable(Constants.RemoteOllamaUrlEnvVar, null);

            _sessionLogger = Substitute.For<IProxySessionLogger>();
            _contextOptimizerFactory = Substitute.For<IContextOptimizerFactory>();
            _sessionRegistry = Substitute.For<ISessionRegistry>();
            _toolCallInspector = Substitute.For<IToolCallInspector>();
            _backendRegistry = Substitute.For<IAiBackendRegistry>();
            _backendRouter = Substitute.For<IBackendRouter>();
            _llamaCppProcessManager = Substitute.For<ILlamaCppProcessManager>();
            _modelScheduler = Substitute.For<IModelScheduler>();
            _openAiRequestParser = Substitute.For<IOpenAiRequestParser>();
            _backendModelSelector = Substitute.For<IBackendModelSelector>();
            _tokenStatsStore = Substitute.For<ITokenStatsStore>();
            _handler = new CapturingHandler();

            List<IContextOptimizer> noOptimizers = [];
            List<IAiModelConfig> noCandidates = [];

            _sessionRegistry.GetOrCreateSessionId(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(TestSessionId);
            _contextOptimizerFactory.GetOptimizers(Arg.Any<string>()).Returns(noOptimizers);
            _backendRouter.ChooseCandidates(Arg.Any<OpenAIModel>(), Arg.Any<RoutingEnvelope>()).Returns(noCandidates);
            _modelScheduler.AcquireGlobalAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IDisposable>(Substitute.For<IDisposable>()));
            _modelScheduler.AcquireAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IDisposable>(Substitute.For<IDisposable>()));
            _toolCallInspector.InspectResponseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<string>(0));

            _backendClientFactory = CreateClientFactory(_handler);
        }

        [TestCleanup]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(Constants.RemoteOllamaUrlEnvVar, _previousRemoteOllamaUrl);
        }

        [TestMethod]
        public void Constructor_NullSessionLogger_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(null, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullContextOptimizerFactory_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, null, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullSessionRegistry_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, null,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullToolCallInspector_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                null, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullBackendRegistry_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, null, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullBackendRouter_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, null, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullLlamaCppProcessManager_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, null, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullBackendClientFactory_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, null, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullModelScheduler_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, null,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullOpenAiRequestParser_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                null, _backendModelSelector, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullBackendModelSelector_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, null, _tokenStatsStore));
        }

        [TestMethod]
        public void Constructor_NullTokenStatsStore_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ProxyController(_sessionLogger, _contextOptimizerFactory, _sessionRegistry,
                _toolCallInspector, _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, null));
        }

        [TestMethod]
        public async Task GetTags_WithBackends_ReturnsTagForEachBackend()
        {
            ConfigureRegistry(CreateBackend());
            ProxyController sut = CreateSut(out _);

            IActionResult result = await sut.GetTags();

            OkObjectResult okResult = (OkObjectResult)result;
            List<TagModel> tags = (List<TagModel>)okResult.Value;
            Assert.HasCount(1, tags);
            Assert.AreEqual(TestAlias, tags[0].name);
            Assert.AreEqual(TestModel, tags[0].model);
        }

        [TestMethod]
        public async Task GetTags_NoBackends_ReturnsEmptyList()
        {
            ConfigureRegistry();
            ProxyController sut = CreateSut(out _);

            IActionResult result = await sut.GetTags();

            OkObjectResult okResult = (OkObjectResult)result;
            List<TagModel> tags = (List<TagModel>)okResult.Value;
            Assert.IsEmpty(tags);
        }

        [TestMethod]
        public async Task ProxyAsync_ModelsRoute_ReturnsBackendList()
        {
            ConfigureRegistry(CreateBackend());
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetGetRequest(context, "/v1/models");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            string body = ReadResponseBody(context);
            Assert.Contains("\"object\":\"list\"", body);
            Assert.Contains(TestAlias, body);
        }

        [TestMethod]
        public async Task ProxyAsync_ModelsRoute_NoBackends_ReturnsEmptyData()
        {
            ConfigureRegistry();
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetGetRequest(context, "/v1/models");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            string body = ReadResponseBody(context);
            Assert.Contains("\"data\":[]", body);
        }

        [TestMethod]
        public async Task ProxyAsync_ShowRoute_KnownModel_ReturnsModelMetadata()
        {
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/show", "{\"model\":\"" + TestAlias + "\"}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            string body = ReadResponseBody(context);
            Assert.Contains(TestAlias, body);
            Assert.Contains("\"modified_at\"", body);
        }

        [TestMethod]
        public async Task ProxyAsync_ShowRoute_UnknownModel_ReturnsBadGateway()
        {
            ConfigureRegistry(CreateBackend());
            ConfigureParser("missing-model");
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/show", "{\"model\":\"missing-model\"}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_EmbeddingsRoute_KnownModel_ReturnsEmbedding()
        {
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/embeddings", "{\"model\":\"" + TestAlias + "\",\"prompt\":\"hello\"}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            string body = ReadResponseBody(context);
            Assert.Contains("\"object\":\"list\"", body);
            Assert.Contains("\"embedding\"", body);
        }

        [TestMethod]
        public async Task ProxyAsync_EmbeddingsRoute_UnknownModel_ReturnsBadGateway()
        {
            ConfigureRegistry(CreateBackend());
            ConfigureParser("missing-model");
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/embeddings", "{\"model\":\"missing-model\",\"prompt\":\"hello\"}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_ChatCompletions_ValidModel_ReturnsChatResponse()
        {
            _handler.Response = CreateJsonResponse(ChatCompletionResponse);
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            Assert.AreEqual(ChatCompletionResponse, ReadResponseBody(context));
            Assert.IsNotNull(_handler.CapturedRequest);
            Assert.AreEqual(HttpMethod.Post, _handler.CapturedRequest.Method);
            Assert.Contains(TestEndpoint, _handler.CapturedRequest.RequestUri.ToString());
        }

        [TestMethod]
        public async Task ProxyAsync_ChatCompletions_StreamingResponse_StreamsEventSource()
        {
            const string sseBody = "data: {\"choices\":[{\"delta\":{\"content\":\"hi\"}}]}\n\n";
            _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sseBody, Encoding.UTF8, "text/event-stream")
            };
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"" + TestAlias + "\",\"stream\":true}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            Assert.AreEqual("text/event-stream", context.Response.ContentType);
            Assert.AreEqual(sseBody, ReadResponseBody(context));
        }

        [TestMethod]
        public async Task ProxyAsync_ChatCompletions_UnknownModel_ReturnsBadGateway()
        {
            ConfigureRegistry(CreateBackend());
            ConfigureParser("missing-model");
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"missing-model\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_GlobalQueueFull_ReturnsTooManyRequests()
        {
            _modelScheduler.AcquireGlobalAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IDisposable>(null));
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_ChatCompletions_BackendUnreachable_ReturnsBadGateway()
        {
            _handler.SendException = new HttpRequestException("connection refused");
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_BackendSlotsUnavailable_ReturnsGatewayTimeout()
        {
            _modelScheduler.AcquireAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IDisposable>(null));
            ConfigureRegistry(CreateBackend());
            ConfigureParser(TestAlias);
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/chat/completions", "{\"model\":\"" + TestAlias + "\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.GatewayTimeout, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ProxyAsync_UnknownRoute_ForwardsToRemote()
        {
            using FakeHttpServer server = new("{\"ok\":true}", "application/json", HttpStatusCode.OK);
            Environment.SetEnvironmentVariable(Constants.RemoteOllamaUrlEnvVar, $"http://127.0.0.1:{server.Port}");

            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/generate", "{\"model\":\"anything\",\"prompt\":\"hi\"}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            Assert.AreEqual("{\"ok\":true}", ReadResponseBody(context));
            Assert.AreEqual("POST /v1/generate HTTP/1.1", server.LastRequestLine);
        }

        [TestMethod]
        public async Task ProxyAsync_LegacyCompletions_TranslatesToChatCompletions()
        {
            using FakeHttpServer server = new("{\"ok\":true}", "application/json", HttpStatusCode.OK);
            Environment.SetEnvironmentVariable(Constants.RemoteOllamaUrlEnvVar, $"http://127.0.0.1:{server.Port}");

            ConfigureRegistry(CreateBackend(alias: "", model: TestModel, endpoint: $"http://127.0.0.1:{server.Port}"));
            ConfigureParser(TestModel, "hi");
            ProxyController sut = CreateSut(out DefaultHttpContext context);
            SetRequest(context, "POST", "/v1/completions", "{\"model\":\"" + TestModel + "\",\"prompt\":\"hi\",\"stream\":false}");

            await sut.ProxyAsync();

            Assert.AreEqual((int)HttpStatusCode.OK, context.Response.StatusCode);
            Assert.AreEqual("POST /v1/chat/completions HTTP/1.1", server.LastRequestLine);
            Assert.Contains("\"messages\"", server.LastRequestBody);
        }

        private ProxyController CreateSut(out DefaultHttpContext context)
        {
            ProxyController sut = new(_sessionLogger, _contextOptimizerFactory, _sessionRegistry, _toolCallInspector,
                _backendRegistry, _backendRouter, _llamaCppProcessManager, _backendClientFactory, _modelScheduler,
                _openAiRequestParser, _backendModelSelector, _tokenStatsStore);

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

        private void ConfigureRegistry(params IAiModelConfig[] backends)
        {
            _backendRegistry.GetAllBackends().Returns(backends);
        }

        private void ConfigureParser(string modelName, string prompt = null)
        {
            _openAiRequestParser.ParseAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(call => (new OpenAIModel { Model = modelName, Prompt = prompt }, (RoutingEnvelope)null, call.ArgAt<string>(0)));
        }

        private static AiModelConfig CreateBackend(string alias = TestAlias, string model = TestModel, string endpoint = TestEndpoint)
        {
            return new AiModelConfig
            {
                Alias = alias,
                Model = model,
                Endpoint = endpoint,
                Kind = ModelKind.Ollama,
                Enabled = true
            };
        }

        private static void SetGetRequest(DefaultHttpContext context, string path)
        {
            context.Request.Method = "GET";
            context.Request.Path = path;
        }

        private static void SetRequest(DefaultHttpContext context, string method, string path, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            context.Request.Method = method;
            context.Request.Path = path;
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bytes.Length;
        }

        private static string ReadResponseBody(DefaultHttpContext context)
        {
            context.Response.Body.Position = 0;
            using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            return reader.ReadToEnd();
        }

        private static HttpResponseMessage CreateJsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage CapturedRequest { get; private set; }

            public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

            public Exception SendException { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedRequest = request;

                if (SendException != null)
                {
                    return Task.FromException<HttpResponseMessage>(SendException);
                }

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

        private sealed class FakeHttpServer : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly Task _serverTask;

            public int Port { get; }

            public string LastRequestLine { get; private set; } = string.Empty;

            public string LastRequestBody { get; private set; } = string.Empty;

            public FakeHttpServer(string responseBody, string contentType, HttpStatusCode statusCode)
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _serverTask = Task.Run(() => Serve(responseBody, contentType, statusCode));
            }

            public void Dispose()
            {
                _listener.Stop();
                try
                {
                    _serverTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch
                {
                }
            }

            private void Serve(string responseBody, string contentType, HttpStatusCode statusCode)
            {
                try
                {
                    using TcpClient client = _listener.AcceptTcpClient();
                    using NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000;

                    using MemoryStream requestBytes = new();
                    byte[] buffer = new byte[8192];
                    int contentLength = 0;
                    bool bodyReceived = false;

                    while (!bodyReceived)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);

                        if (read <= 0)
                            break;

                        requestBytes.Write(buffer, 0, read);

                        string soFar = Encoding.UTF8.GetString(requestBytes.ToArray());
                        int headerEnd = soFar.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                        if (headerEnd < 0)
                            continue;

                        contentLength = ReadContentLength(soFar[..headerEnd]);
                        int bodyStart = headerEnd + 4;
                        bodyReceived = soFar.Length - bodyStart >= contentLength;
                    }

                    string requestText = Encoding.UTF8.GetString(requestBytes.ToArray());
                    int lineEnd = requestText.IndexOf("\r\n", StringComparison.Ordinal);
                    LastRequestLine = lineEnd >= 0 ? requestText[..lineEnd] : requestText;

                    int bodyIndex = requestText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    LastRequestBody = bodyIndex >= 0 ? requestText[(bodyIndex + 4)..] : string.Empty;

                    byte[] body = Encoding.UTF8.GetBytes(responseBody);
                    string headers = $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
                                     $"Content-Type: {contentType}\r\n" +
                                     $"Content-Length: {body.Length}\r\n" +
                                     "Connection: close\r\n\r\n";

                    stream.Write(Encoding.UTF8.GetBytes(headers));
                    stream.Write(body);
                    stream.Flush();
                }
                catch
                {
                }
            }

            private static int ReadContentLength(string headerText)
            {
                foreach (string line in headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = line.IndexOf(':');

                    if (colon > 0 && line[..colon].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(line[(colon + 1)..].Trim(), out int value))
                        {
                            return value;
                        }
                    }
                }

                return 0;
            }
        }
    }
}
