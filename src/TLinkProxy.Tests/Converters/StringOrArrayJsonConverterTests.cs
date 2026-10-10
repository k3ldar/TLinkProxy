using TLinkProxy.Kernel.Converters;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Tests.Converters
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class StringOrArrayJsonConverterTests
    {
        [TestMethod]
        public void Read_StringValue_ReturnsString()
        {
            string result = Read("\"hello\"");

            Assert.AreEqual("hello", result);
        }

        [TestMethod]
        public void Read_EmptyStringValue_ReturnsEmptyString()
        {
            string result = Read("\"\"");

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Read_ArrayWithSingleString_ReturnsFirstElement()
        {
            string result = Read("[\"only\"]");

            Assert.AreEqual("only", result);
        }

        [TestMethod]
        public void Read_ArrayWithMultipleStrings_ReturnsFirstElement()
        {
            string result = Read("[\"first\",\"second\",\"third\"]");

            Assert.AreEqual("first", result);
        }

        [TestMethod]
        public void Read_EmptyArray_ReturnsEmptyString()
        {
            string result = Read("[]");

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Read_ArrayWithNumberFirst_ReturnsNumberAsString()
        {
            string result = Read("[42]");

            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void Read_ArrayWithBooleanFirst_ReturnsBooleanAsString()
        {
            string result = Read("[true]");

            Assert.AreEqual("True", result);
        }

        [TestMethod]
        public void Read_ArrayWithNullFirst_ReturnsEmptyString()
        {
            string result = Read("[null]");

            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void Read_ArrayWithObjectFirst_ReturnsRawObjectJson()
        {
            string result = Read("[{\"key\":\"value\"}]");

            Assert.AreEqual("{\"key\":\"value\"}", result);
        }

        [TestMethod]
        public void Read_NumberTopLevel_ReturnsRawNumberText()
        {
            string result = Read("42");

            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void Read_BooleanTopLevel_ReturnsRawBooleanText()
        {
            string result = Read("true");

            Assert.AreEqual("true", result);
        }

        [TestMethod]
        public void Read_NullTopLevel_ReturnsRawNullText()
        {
            string result = Read("null");

            Assert.AreEqual("null", result);
        }

        [TestMethod]
        public void Read_ObjectTopLevel_ReturnsRawObjectText()
        {
            string result = Read("{\"a\":1}");

            Assert.AreEqual("{\"a\":1}", result);
        }

        [TestMethod]
        public void Deserialize_StringProperty_ReturnsString()
        {
            TestDto sut = JsonSerializer.Deserialize<TestDto>("{\"Value\":\"hello\"}");

            Assert.IsNotNull(sut);
            Assert.AreEqual("hello", sut.Value);
        }

        [TestMethod]
        public void Deserialize_ArrayProperty_ReturnsFirstElement()
        {
            TestDto sut = JsonSerializer.Deserialize<TestDto>("{\"Value\":[\"first\",\"second\"]}");

            Assert.IsNotNull(sut);
            Assert.AreEqual("first", sut.Value);
        }

        [TestMethod]
        public void Serialize_StringValue_WritesJsonString()
        {
            string json = Write("hello");

            Assert.AreEqual("\"hello\"", json);
        }

        [TestMethod]
        public void Serialize_EmptyString_WritesEmptyJsonString()
        {
            string json = Write("");

            Assert.AreEqual("\"\"", json);
        }

        [TestMethod]
        public void Serialize_NullValue_WritesNull()
        {
            string json = Write(null);

            Assert.AreEqual("null", json);
        }

        private static string Read(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            Utf8JsonReader reader = new(bytes);
            Assert.IsTrue(reader.Read(), "Expected at least one JSON token.");
            return new StringOrArrayJsonConverter().Read(ref reader, typeof(string), null);
        }

        private static string Write(string value)
        {
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                new StringOrArrayJsonConverter().Write(writer, value, null);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private sealed class TestDto
        {
            [JsonConverter(typeof(StringOrArrayJsonConverter))]
            public string Value { get; set; }
        }
    }
}
