using System.Diagnostics.CodeAnalysis;

using TLinkProxy.Kernel;
using TLinkProxy.Services;

namespace TLinkProxy.Tests.Services
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class OpenAiRequestParserTests
    {
        [TestMethod]
        public async Task ParseAsync_ArrayMessageContent_FlattensTextAndDoesNotThrow()
        {
            var parser = new OpenAiRequestParser();
            const string body = """
            {
              "model": "deepseek-v4-pro",
              "messages": [
                { "role": "system", "content": "" },
                { "role": "user", "content": [ { "type": "text", "text": "hello " }, { "type": "text", "text": "world" } ] }
              ]
            }
            """;

            (OpenAIModel model, RoutingEnvelope routing, string normalizedBody) = await parser.ParseAsync(body, "application/json");

            Assert.IsNotNull(normalizedBody);
            Assert.IsNull(routing);
            Assert.IsNotNull(model);
            Assert.AreEqual("deepseek-v4-pro", model.Model);
            Assert.IsNotNull(model.Messages);
            Assert.HasCount(2, model.Messages);
            Assert.AreEqual("hello world", model.Messages[1].Content);
        }

        [TestMethod]
        public async Task ParseAsync_RoutingSystemMessage_StillExtractsRouting()
        {
            var parser = new OpenAiRequestParser();
            const string body = """
            {
              "model": "deepseek-v4-pro",
              "messages": [
                { "role": "system", "content": "[[OLLAMAPROXY_ROUTING]]{\"requestType\":\"chat\"}" },
                { "role": "user", "content": "hello" }
              ]
            }
            """;

            (OpenAIModel model, RoutingEnvelope routing, string _) = await parser.ParseAsync(body, "application/json");

            Assert.IsNotNull(routing);
            Assert.AreEqual("chat", routing.RequestType);
            Assert.IsNotNull(model);
            Assert.AreEqual("deepseek-v4-pro", model.Model);
        }

        [TestMethod]
        public async Task ParseAsync_UnmappableField_PreservesModelNameForRouting()
        {
            var parser = new OpenAiRequestParser();
            const string body = """
            {
              "model": "deepseek-v4-pro",
              "stream": "not-a-boolean",
              "messages": [ { "role": "user", "content": "hello" } ]
            }
            """;

            (OpenAIModel model, RoutingEnvelope routing, string normalizedBody) = await parser.ParseAsync(body, "application/json");

            Assert.IsNotNull(normalizedBody);
            Assert.IsNull(routing);
            Assert.IsNotNull(model);
            Assert.AreEqual("deepseek-v4-pro", model.Model);
            Assert.IsNull(model.Messages);
        }
    }
}
