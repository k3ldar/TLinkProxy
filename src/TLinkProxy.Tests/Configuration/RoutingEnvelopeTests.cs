using TLinkProxy.Base;
using TLinkProxy.Kernel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TLinkProxy.Tests.Configuration
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class RoutingEnvelopeTests
    {
        [TestMethod]
        public void Deserialize_ValidJson_MapsAllProperties()
        {
            const string json = """
            {
              "requestType": "chat",
              "requiredCapabilities": ["reasoning", "vision"],
              "timeoutMs": 5000,
              "preferredModel": "gpt-4o",
              "payload": { "message": "hello", "temperature": 0.7 }
            }
            """;

            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.AreEqual("chat", sut.RequestType);
            Assert.AreEqual("gpt-4o", sut.PreferredModel);
            Assert.AreEqual(5000, sut.TimeoutMs);
            CollectionAssert.AreEqual(new[] { "reasoning", "vision" }, sut.RequiredCapabilities);
            Assert.IsTrue(sut.Payload.HasValue);
            Assert.AreEqual("hello", sut.Payload.Value.GetProperty("message").GetString());
            Assert.AreEqual(0.7, sut.Payload.Value.GetProperty("temperature").GetDouble());
        }

        [TestMethod]
        public void Deserialize_MinimalJson_LeavesOptionalPropertiesNull()
        {
            const string json = """{ "requestType": "chat" }""";

            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.AreEqual("chat", sut.RequestType);
            Assert.IsNull(sut.RequiredCapabilities);
            Assert.IsNull(sut.TimeoutMs);
            Assert.IsNull(sut.PreferredModel);
            Assert.IsNull(sut.Payload);
        }

        [TestMethod]
        public void Deserialize_EmptyRequiredCapabilities_ReturnsEmptyArray()
        {
            const string json = """{ "requestType": "chat", "requiredCapabilities": [] }""";

            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.IsNotNull(sut.RequiredCapabilities);
            Assert.IsEmpty(sut.RequiredCapabilities);
        }

        [TestMethod]
        public void Deserialize_NullPayload_ReturnsNullPayload()
        {
            const string json = """{ "requestType": "chat", "payload": null }""";

            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.IsNull(sut.Payload);
        }

        [TestMethod]
        public void Deserialize_NullJson_ReturnsNull()
        {
            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>("null", Constants.JsonRequestOptions);

            Assert.IsNull(sut);
        }

        [TestMethod]
        [SuppressMessage("Style", "JSON001:Invalid JSON pattern", Justification = "part of the unit test")]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<RoutingEnvelope>("not-json", Constants.JsonRequestOptions));
        }

        [TestMethod]
        public void Deserialize_WrongPropertyType_ThrowsJsonException()
        {
            const string json = """{ "requestType": "chat", "timeoutMs": "not-a-number" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions));
        }

        [TestMethod]
        public void Serialize_FullyPopulatedEnvelope_ProducesCamelCaseJson()
        {
            RoutingEnvelope sut = new()
            {
                RequestType = "chat",
                RequiredCapabilities = ["reasoning", "vision"],
                TimeoutMs = 5000,
                PreferredModel = "gpt-4o",
                Payload = JsonSerializer.SerializeToElement(new { message = "hello" })
            };

            string json = JsonSerializer.Serialize(sut);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual("chat", root.GetProperty("requestType").GetString());
            Assert.AreEqual("gpt-4o", root.GetProperty("preferredModel").GetString());
            Assert.AreEqual(5000, root.GetProperty("timeoutMs").GetInt32());
            JsonElement capabilities = root.GetProperty("requiredCapabilities");
            Assert.AreEqual(2, capabilities.GetArrayLength());
            Assert.AreEqual("reasoning", capabilities[0].GetString());
            Assert.AreEqual("vision", capabilities[1].GetString());
            Assert.AreEqual("hello", root.GetProperty("payload").GetProperty("message").GetString());
        }

        [TestMethod]
        public void Serialize_NullOptionalValues_OmitsThemFromJson()
        {
            RoutingEnvelope sut = new()
            {
                RequestType = "chat",
                RequiredCapabilities = ["reasoning"],
                TimeoutMs = null,
                PreferredModel = null,
                Payload = null
            };

            string json = JsonSerializer.Serialize(sut, Constants.JsonRequestOptions);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.IsFalse(root.TryGetProperty("timeoutMs", out _));
            Assert.IsFalse(root.TryGetProperty("preferredModel", out _));
            Assert.IsFalse(root.TryGetProperty("payload", out _));
            Assert.AreEqual("chat", root.GetProperty("requestType").GetString());
        }

        [TestMethod]
        public void RoundTrip_SerializeThenDeserialize_PreservesValues()
        {
            RoutingEnvelope original = new()
            {
                RequestType = "chat",
                RequiredCapabilities = ["reasoning", "vision"],
                TimeoutMs = 7500,
                PreferredModel = "gpt-4o",
                Payload = JsonSerializer.SerializeToElement(new { message = "round-trip" })
            };

            string json = JsonSerializer.Serialize(original, Constants.JsonRequestOptions);
            RoutingEnvelope sut = JsonSerializer.Deserialize<RoutingEnvelope>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.AreEqual(original.RequestType, sut.RequestType);
            Assert.AreEqual(original.PreferredModel, sut.PreferredModel);
            Assert.AreEqual(original.TimeoutMs, sut.TimeoutMs);
            CollectionAssert.AreEqual(original.RequiredCapabilities, sut.RequiredCapabilities);
            Assert.IsTrue(sut.Payload.HasValue);
            Assert.AreEqual("round-trip", sut.Payload.Value.GetProperty("message").GetString());
        }
    }
}
