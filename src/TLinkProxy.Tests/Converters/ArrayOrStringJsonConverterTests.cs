using TLinkProxy.Kernel.Converters;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Tests.Converters
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class ArrayOrStringJsonConverterTests
    {
        [TestMethod]
        public void Read_StringValue_ReturnsSingleElementArray()
        {
            string[] result = Read("\"hello\"");

            CollectionAssert.AreEqual(new[] { "hello" }, result);
        }

        [TestMethod]
        public void Read_EmptyStringValue_ReturnsArrayWithEmptyString()
        {
            string[] result = Read("\"\"");

            CollectionAssert.AreEqual(new[] { "" }, result);
        }

        [TestMethod]
        public void Read_ArrayOfStrings_ReturnsAllElements()
        {
            string[] result = Read("[\"a\",\"b\",\"c\"]");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result);
        }

        [TestMethod]
        public void Read_ArrayWithSingleString_ReturnsSingleElementArray()
        {
            string[] result = Read("[\"only\"]");

            CollectionAssert.AreEqual(new[] { "only" }, result);
        }

        [TestMethod]
        public void Read_EmptyArray_ReturnsEmptyArray()
        {
            string[] result = Read("[]");

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Read_ArrayWithNull_ReturnsArrayWithNullElement()
        {
            string[] result = Read("[\"a\",null,\"c\"]");

            Assert.IsNotNull(result);
            Assert.HasCount(3, result);
            Assert.AreEqual("a", result[0]);
            Assert.IsNull(result[1]);
            Assert.AreEqual("c", result[2]);
        }

        [TestMethod]
        public void Read_ArrayWithOnlyNull_ReturnsArrayWithNullElement()
        {
            string[] result = Read("[null]");

            Assert.IsNotNull(result);
            Assert.HasCount(1, result);
            Assert.IsNull(result[0]);
        }

        [TestMethod]
        public void Read_ArrayWithNonStringScalars_IgnoresThem()
        {
            string[] result = Read("[\"keep\",42,true,3.14]");

            CollectionAssert.AreEqual(new[] { "keep" }, result);
        }

        [TestMethod]
        public void Read_ArrayWithNestedArray_IgnoresNestedStrings()
        {
            string[] result = Read("[\"keep\",[\"inner1\",\"inner2\"]]");

            CollectionAssert.AreEqual(new[] { "keep" }, result);
        }

        [TestMethod]
        public void Read_ArrayWithNestedObject_IgnoresNestedStrings()
        {
            string[] result = Read("[\"keep\",{\"nested\":\"inner\"}]");

            CollectionAssert.AreEqual(new[] { "keep" }, result);
        }

        [TestMethod]
        public void Read_ArrayWithDeeplyNestedStrings_IgnoresAllNestedTokens()
        {
            string[] result = Read("[\"keep\",[\"a\",{\"b\":[\"c\"]}]]");

            CollectionAssert.AreEqual(new[] { "keep" }, result);
        }

        [TestMethod]
        public void Read_NumberTopLevel_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(() => Read("42"));
        }

        [TestMethod]
        public void Read_BooleanTopLevel_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(() => Read("true"));
        }

        [TestMethod]
        public void Read_NullTopLevel_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(() => Read("null"));
        }

        [TestMethod]
        public void Read_ObjectTopLevel_ThrowsJsonException()
        {
            Assert.ThrowsExactly<JsonException>(() => Read("{\"a\":1}"));
        }

        [TestMethod]
        public void Deserialize_StringProperty_ReturnsSingleElementArray()
        {
            TestDto sut = JsonSerializer.Deserialize<TestDto>("{\"Value\":\"hello\"}");

            Assert.IsNotNull(sut);
            CollectionAssert.AreEqual(new[] { "hello" }, sut.Value);
        }

        [TestMethod]
        public void Deserialize_ArrayProperty_ReturnsAllElements()
        {
            TestDto sut = JsonSerializer.Deserialize<TestDto>("{\"Value\":[\"a\",\"b\"]}");

            Assert.IsNotNull(sut);
            CollectionAssert.AreEqual(new[] { "a", "b" }, sut.Value);
        }

        [TestMethod]
        public void Serialize_NullValue_WritesNull()
        {
            string json = Write(null);

            Assert.AreEqual("null", json);
        }

        [TestMethod]
        public void Serialize_SingleElement_WritesStringNotArray()
        {
            string json = Write(["hello"]);

            Assert.AreEqual("\"hello\"", json);
        }

        [TestMethod]
        public void Serialize_EmptyArray_WritesEmptyArray()
        {
            string json = Write([]);

            Assert.AreEqual("[]", json);
        }

        [TestMethod]
        public void Serialize_MultipleElements_WritesArray()
        {
            string json = Write(["a", "b", "c"]);

            Assert.AreEqual("[\"a\",\"b\",\"c\"]", json);
        }

        [TestMethod]
        public void Serialize_ArrayWithNullElement_WritesArrayWithNull()
        {
            string json = Write(["a", null]);

            Assert.AreEqual("[\"a\",null]", json);
        }

        private static string[] Read(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            Utf8JsonReader reader = new(bytes);
            Assert.IsTrue(reader.Read(), "Expected at least one JSON token.");
            return new ArrayOrStringJsonConverter().Read(ref reader, typeof(string[]), null);
        }

        private static string Write(string[] value)
        {
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                new ArrayOrStringJsonConverter().Write(writer, value, null);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private sealed class TestDto
        {
            [JsonConverter(typeof(ArrayOrStringJsonConverter))]
            public string[] Value { get; set; }
        }
    }
}
