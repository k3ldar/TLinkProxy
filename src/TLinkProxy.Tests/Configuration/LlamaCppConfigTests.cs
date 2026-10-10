using TLinkProxy.Base.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TLinkProxy.Tests.Configuration
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class LlamaCppConfigTests
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
              "host": "127.0.0.1",
              "port": 8080,
              "gpuLayers": 32,
              "threads": 8,
              "batchSize": 512,
              "contextSize": 8192,
              "autoRestart": true,
              "healthCheckSeconds": 15
            }
            """;

            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(32, sut.GpuLayers);
            Assert.AreEqual(8, sut.Threads);
            Assert.AreEqual(512, sut.BatchSize);
            Assert.IsTrue(sut.AutoRestart);
        }

        [TestMethod]
        public void Deserialize_EmptyObject_LeavesValueTypesAtDefaults()
        {
            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>("{}", Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(0, sut.GpuLayers);
            Assert.AreEqual(0, sut.Threads);
            Assert.AreEqual(0, sut.BatchSize);
            Assert.IsFalse(sut.AutoRestart);
            Assert.AreEqual(0, sut.TimeoutSeconds);
        }

        [TestMethod]
        public void Deserialize_NullJson_ReturnsNull()
        {
            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>("null", Options);

            Assert.IsNull(sut);
        }

        [TestMethod]
        public void Deserialize_PascalCaseProperties_MatchesCaseInsensitively()
        {
            const string json = """
            {
              "Host": "127.0.0.1",
              "Port": 8080,
              "GpuLayers": 32,
              "Threads": 8,
              "BatchSize": 512,
              "ContextSize": 8192,
              "AutoRestart": true,
              "HealthCheckSeconds": 15
            }
            """;

            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(32, sut.GpuLayers);
            Assert.IsTrue(sut.AutoRestart);
        }

        [TestMethod]
        public void Deserialize_NullValueTypes_ThrowJsonException()
        {
            const string json = """
            {
              "gpuLayers": null,
              "threads": null,
              "batchSize": null,
              "contextSize": null,
              "autoRestart": null,
              "healthCheckSeconds": null
            }
            """;

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<LlamaCppConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_AutoRestartFalse_ReturnsFalse()
        {
            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>(
                """{ "autoRestart": false }""", Options);

            Assert.IsNotNull(sut);
            Assert.IsFalse(sut.AutoRestart);
        }

        [TestMethod]
        public void Deserialize_NegativeIntegers_ArePreserved()
        {
            const string json = """{ "gpuLayers": -1, "threads": -2, "batchSize": -3, "contextSize": -4 }""";

            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(-1, sut.GpuLayers);
            Assert.AreEqual(-2, sut.Threads);
            Assert.AreEqual(-3, sut.BatchSize);
        }

        [TestMethod]
        [SuppressMessage("Style", "JSON001:Invalid JSON pattern", Justification = "Part of the unit test")]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<LlamaCppConfig>("not-json", Options));
        }

        [TestMethod]
        public void Deserialize_WrongTypeForPort_ThrowsJsonException()
        {
            const string json = """{ "port": "not-a-number" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<LlamaCppConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_StringForInt_ThrowsJsonException()
        {
            const string json = """{ "gpuLayers": "thirty-two" }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<LlamaCppConfig>(json, Options));
        }

        [TestMethod]
        public void Deserialize_IntOverflow_ThrowsJsonException()
        {
            const string json = """{ "port": 2147483648 }""";

            Assert.ThrowsExactly<JsonException>(
                () => JsonSerializer.Deserialize<LlamaCppConfig>(json, Options));
        }

        [TestMethod]
        public void Serialize_FullyPopulatedConfig_ProducesCamelCaseJson()
        {
            LlamaCppConfig sut = new()
            {
                GpuLayers = 32,
                Threads = 8,
                BatchSize = 512,
                AutoRestart = true,
            };

            string json = JsonSerializer.Serialize(sut, Options);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual("127.0.0.1", root.GetProperty("host").GetString());
            Assert.AreEqual(8080, root.GetProperty("port").GetInt32());
            Assert.AreEqual(32, root.GetProperty("gpuLayers").GetInt32());
            Assert.AreEqual(8, root.GetProperty("threads").GetInt32());
            Assert.AreEqual(512, root.GetProperty("batchSize").GetInt32());
            Assert.AreEqual(8192, root.GetProperty("contextSize").GetInt32());
            Assert.IsTrue(root.GetProperty("autoRestart").GetBoolean());
            Assert.AreEqual(15, root.GetProperty("healthCheckSeconds").GetInt32());
        }

        [TestMethod]
        public void RoundTrip_SerializeThenDeserialize_PreservesValues()
        {
            LlamaCppConfig original = new()
            {
                GpuLayers = 64,
                Threads = 4,
                BatchSize = 256,
                AutoRestart = false,
            };

            string json = JsonSerializer.Serialize(original, Options);
            LlamaCppConfig sut = JsonSerializer.Deserialize<LlamaCppConfig>(json, Options);

            Assert.IsNotNull(sut);
            Assert.AreEqual(original.GpuLayers, sut.GpuLayers);
            Assert.AreEqual(original.Threads, sut.Threads);
            Assert.AreEqual(original.BatchSize, sut.BatchSize);
            Assert.AreEqual(original.AutoRestart, sut.AutoRestart);
        }
    }
}
