using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TLinkProxy.Tests.Configuration
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class BackendConfigTests
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        [TestMethod]
        public void Deserialize_FullJson_MapsAllProperties()
        {
            const string json = """
            {
              "alias": "alpha",
              "kind": "ollama",
              "endpoint": "http://localhost:11434",
              "model": "llama3.1",
              "tags": ["fast", "cpu"],
              "compute": "cpu",
              "latencyProfile": "balanced",
              "costPer1kInput": 1.25,
              "costPer1kOutput": 2.5,
              "maxContext": 8192,
              "concurrencyLimit": 4,
              "weight": 10,
              "apiKeyEnvVar": "MY_API_KEY",
              "allowQueue": true,
              "maxQueueDepth": 12,
              "enabled": true
            }
            """;

            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual("alpha", sut.Alias);
            Assert.AreEqual(ModelKind.Ollama, sut.Kind);
            Assert.AreEqual("http://localhost:11434", sut.Endpoint);
            Assert.AreEqual("llama3.1", sut.Model);
            CollectionAssert.AreEqual(new[] { "fast", "cpu" }, sut.Tags);
            Assert.AreEqual("cpu", sut.Compute);
            Assert.AreEqual("balanced", sut.LatencyProfile);
            Assert.AreEqual(1.25, sut.CostPer1mInput);
            Assert.AreEqual(2.5, sut.CostPer1mOutput);
            Assert.AreEqual(8192, sut.MaxContext);
            Assert.AreEqual(4, sut.ConcurrencyLimit);
            Assert.AreEqual(10, sut.Weight);
            Assert.AreEqual("MY_API_KEY", sut.ApiKeyEnvVar);
            Assert.IsTrue(sut.AllowQueue);
            Assert.AreEqual(12, sut.MaxQueueDepth);
            Assert.IsTrue(sut.Enabled);
        }

        [TestMethod]
        public void Deserialize_EmptyObject_LeavesReferenceTypesNullAndValueTypesAtDefaults()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>("{}", Options);

            Assert.IsNotNull(sut);
            Assert.IsNull(sut.Alias);
            Assert.IsNull(sut.Endpoint);
            Assert.IsNull(sut.Model);
            Assert.IsNull(sut.Tags);
            Assert.IsNull(sut.Compute);
            Assert.IsNull(sut.LatencyProfile);
            Assert.IsNull(sut.ApiKeyEnvVar);
            Assert.AreEqual(0.0, sut.CostPer1mInput);
            Assert.AreEqual(0.0, sut.CostPer1mOutput);
            Assert.AreEqual(0, sut.MaxContext);
            Assert.AreEqual(Constants.DefaultConcurrencyLimit, sut.ConcurrencyLimit);
            Assert.AreEqual(Constants.DefaultWeight, sut.Weight);
            Assert.IsTrue(sut.AllowQueue);
            Assert.AreEqual(Constants.DefaultQueueDepth, sut.MaxQueueDepth);
            Assert.IsTrue(sut.Enabled);
        }

        [TestMethod]
        public void Deserialize_NullJson_ReturnsNull()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>("null", Options);

            Assert.IsNull(sut);
        }

        [TestMethod]
        public void Deserialize_EmptyTags_ReturnsEmptyArray()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(
                """{ "alias": "alpha", "tags": [] }""", Options);

            Assert.IsNotNull(sut);
            Assert.IsNotNull(sut.Tags);
            Assert.IsEmpty(sut.Tags);
        }

        [TestMethod]
        public void Deserialize_NullTags_ReturnsNull()
        {
            const string json = """{ "alias": "alpha", "tags": null }""";

            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual("alpha", sut.Alias);
            Assert.IsNull(sut.Tags);
        }

        [TestMethod]
        public void Deserialize_NullValueTypes_ThrowJsonException()
        {
            const string json = """{ "costPer1kInput": null }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_PascalCaseProperties_MatchCaseInsensitively()
        {
            const string json = """
            {
              "Alias": "alpha",
              "Kind": "ollama",
              "Endpoint": "http://localhost:11434",
              "Model": "llama3",
              "Tags": ["fast"],
              "Compute": "gpu",
              "LatencyProfile": "low",
              "CostPer1kInput": 1.5,
              "CostPer1kOutput": 3.0,
              "MaxContext": 4096,
              "ConcurrencyLimit": 2,
              "Weight": 5,
              "ApiKeyEnvVar": "KEY",
              "AllowQueue": false,
              "MaxQueueDepth": 3
            }
            """;

            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual("alpha", sut.Alias);
            Assert.AreEqual("gpu", sut.Compute);
            Assert.AreEqual(1.5, sut.CostPer1mInput);
            Assert.IsFalse(sut.AllowQueue);
            Assert.AreEqual(3, sut.MaxQueueDepth);
        }

        [TestMethod]
        public void Deserialize_UnknownProperties_AreIgnored()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(
                """{ "alias": "alpha", "futureField": "ignored", "nested": { "a": 1 } }""", Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual("alpha", sut.Alias);
        }

        [TestMethod]
        public void Deserialize_AllowQueueFalse_ReturnsFalse()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(
                """{ "allowQueue": false }""", Options);

            Assert.IsNotNull(sut);
            Assert.IsFalse(sut.AllowQueue);
        }

        [TestMethod]
        public void Deserialize_EnabledFalse_ReturnsFalse()
        {
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(
                """{ "enabled": false }""", Options);

            Assert.IsNotNull(sut);
            Assert.IsFalse(sut.Enabled);
        }

        [TestMethod]
        public void Deserialize_CostValues_HandleWholeNumbersAndFractions()
        {
            const string json = """{ "costPer1kInput": 2, "costPer1kOutput": 0.001 }""";

            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(2.0, sut.CostPer1mInput);
            Assert.AreEqual(0.001, sut.CostPer1mOutput);
        }

        [TestMethod]
        public void Deserialize_NegativeNumericValues_ArePreserved()
        {
            const string json = """{ "costPer1kInput": -1.5, "maxContext": -1, "weight": -10 }""";

            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(-1.5, sut.CostPer1mInput);
            Assert.AreEqual(-1, sut.MaxContext);
            Assert.AreEqual(-10, sut.Weight);
        }

        [TestMethod]
        [SuppressMessage("Style", "JSON001:Invalid JSON pattern", Justification = "Part of the test")]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>("not-json", Options));
        }

        [TestMethod]
        public void Deserialize_WrongTypeForInt_ThrowsJsonException()
        {
            const string json = """{ "maxContext": "eight-thousand" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_WrongTypeForDouble_ThrowsJsonException()
        {
            const string json = """{ "costPer1kInput": "cheap" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_WrongTypeForBool_ThrowsJsonException()
        {
            const string json = """{ "allowQueue": "yes" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_ObjectForTags_ThrowsJsonException()
        {
            const string json = """{ "tags": { "name": "fast" } }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<AiModelConfig>(json, Options));
        }

        [TestMethod]
        public void Serialize_FullyPopulatedConfig_ProducesCamelCaseJson()
        {
            AiModelConfig sut = new()
            {
                Alias = "alpha",
                Kind = ModelKind.Ollama,
                Endpoint = "http://localhost:11434",
                Model = "llama3.1",
                Tags = ["fast", "cpu"],
                Compute = "cpu",
                LatencyProfile = "balanced",
                CostPer1mInput = 1.25,
                CostPer1mOutput = 2.5,
                MaxContext = 8192,
                ConcurrencyLimit = 4,
                Weight = 10,
                ApiKeyEnvVar = "MY_API_KEY",
                AllowQueue = true,
                MaxQueueDepth = 12
            };

            string json = JsonSerializer.Serialize(sut, Options);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual("alpha", root.GetProperty("alias").GetString());
            Assert.AreEqual("ollama", root.GetProperty("kind").GetString());
            Assert.AreEqual("http://localhost:11434", root.GetProperty("endpoint").GetString());
            Assert.AreEqual("llama3.1", root.GetProperty("model").GetString());
            Assert.AreEqual("cpu", root.GetProperty("compute").GetString());
            Assert.AreEqual("balanced", root.GetProperty("latencyProfile").GetString());
            Assert.AreEqual(1.25, root.GetProperty("costPer1kInput").GetDouble());
            Assert.AreEqual(2.5, root.GetProperty("costPer1kOutput").GetDouble());
            Assert.AreEqual(8192, root.GetProperty("maxContext").GetInt32());
            Assert.AreEqual(4, root.GetProperty("concurrencyLimit").GetInt32());
            Assert.AreEqual(10, root.GetProperty("weight").GetInt32());
            Assert.AreEqual("MY_API_KEY", root.GetProperty("apiKeyEnvVar").GetString());
            Assert.IsTrue(root.GetProperty("allowQueue").GetBoolean());
            Assert.AreEqual(12, root.GetProperty("maxQueueDepth").GetInt32());

            JsonElement tags = root.GetProperty("tags");
            Assert.AreEqual(2, tags.GetArrayLength());
            Assert.AreEqual("fast", tags[0].GetString());
            Assert.AreEqual("cpu", tags[1].GetString());
        }

        [TestMethod]
        public void Serialize_Defaults_ReferenceTypesOmittedValueTypesWritten()
        {
            AiModelConfig sut = new()
            {
                Alias = "alpha",
                Kind = ModelKind.Ollama,
                Endpoint = "http://localhost:11434"
            };

            string json = JsonSerializer.Serialize(sut, Options);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.IsFalse(root.TryGetProperty("model", out _));
            Assert.IsFalse(root.TryGetProperty("tags", out _));
            Assert.IsFalse(root.TryGetProperty("compute", out _));
            Assert.IsFalse(root.TryGetProperty("latencyProfile", out _));
            Assert.IsFalse(root.TryGetProperty("apiKeyEnvVar", out _));
            Assert.AreEqual("alpha", root.GetProperty("alias").GetString());
            Assert.AreEqual(0.0, root.GetProperty("costPer1kInput").GetDouble());
            Assert.AreEqual(0.0, root.GetProperty("costPer1kOutput").GetDouble());
            Assert.AreEqual(0, root.GetProperty("maxContext").GetInt32());
            Assert.AreEqual(Constants.DefaultConcurrencyLimit, root.GetProperty("concurrencyLimit").GetInt32());
            Assert.AreEqual(Constants.DefaultWeight, root.GetProperty("weight").GetInt32());
            Assert.IsTrue(root.GetProperty("allowQueue").GetBoolean());
            Assert.AreEqual(Constants.DefaultQueueDepth, root.GetProperty("maxQueueDepth").GetInt32());
            Assert.IsTrue(root.GetProperty("enabled").GetBoolean());
        }

        [TestMethod]
        public void RoundTrip_SerializeThenDeserialize_PreservesValues()
        {
            AiModelConfig original = new()
            {
                Alias = "beta",
                Kind = ModelKind.OpenAI,
                Endpoint = "https://api.example.com",
                Model = "gpt-4o",
                Tags = ["reasoning", "vision"],
                Compute = "gpu",
                LatencyProfile = "low",
                CostPer1mInput = 5.0,
                CostPer1mOutput = 15.0,
                MaxContext = 128000,
                ConcurrencyLimit = 8,
                Weight = 3,
                ApiKeyEnvVar = "OPENAI_API_KEY",
                AllowQueue = false,
                MaxQueueDepth = 0
            };

            string json = JsonSerializer.Serialize(original, Options);
            AiModelConfig sut = JsonSerializer.Deserialize<AiModelConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(original.Alias, sut.Alias);
            Assert.AreEqual(original.Kind, sut.Kind);
            Assert.AreEqual(original.Endpoint, sut.Endpoint);
            Assert.AreEqual(original.Model, sut.Model);
            CollectionAssert.AreEqual(original.Tags, sut.Tags);
            Assert.AreEqual(original.Compute, sut.Compute);
            Assert.AreEqual(original.LatencyProfile, sut.LatencyProfile);
            Assert.AreEqual(original.CostPer1mInput, sut.CostPer1mInput);
            Assert.AreEqual(original.CostPer1mOutput, sut.CostPer1mOutput);
            Assert.AreEqual(original.MaxContext, sut.MaxContext);
            Assert.AreEqual(original.ConcurrencyLimit, sut.ConcurrencyLimit);
            Assert.AreEqual(original.Weight, sut.Weight);
            Assert.AreEqual(original.ApiKeyEnvVar, sut.ApiKeyEnvVar);
            Assert.AreEqual(original.AllowQueue, sut.AllowQueue);
            Assert.AreEqual(original.MaxQueueDepth, sut.MaxQueueDepth);
        }
    }
}
