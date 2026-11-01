using TLinkProxy.Kernel.Converters;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Tests.Converters
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class MessageContentJsonConverterTests
    {
        [TestMethod]
        public void Read_String_ReturnsString()
        {
            Assert.AreEqual("hello", Read("\"hello\""));
        }

        [TestMethod]
        public void Read_Null_ReturnsNull()
        {
            Assert.IsNull(Read("null"));
        }

        [TestMethod]
        public void Read_EmptyArray_ReturnsEmptyString()
        {
            Assert.AreEqual("", Read("[]"));
        }

        [TestMethod]
        public void Read_ArrayOfStrings_Concatenates()
        {
            Assert.AreEqual("ab", Read("[\"a\",\"b\"]"));
        }

        [TestMethod]
        public void Read_ArrayOfTextObjects_ConcatenatesTextFields()
        {
            Assert.AreEqual("firstsecond", Read("[{\"type\":\"text\",\"text\":\"first\"},{\"type\":\"text\",\"text\":\"second\"}]"));
        }

        [TestMethod]
        public void Read_ArrayWithObjectWithoutText_ReturnsRawObjectJson()
        {
            Assert.AreEqual("{\"key\":\"value\"}", Read("[{\"key\":\"value\"}]"));
        }

        [TestMethod]
        public void Read_ArrayWithNull_IgnoresNull()
        {
            Assert.AreEqual("ab", Read("[\"a\",null,\"b\"]"));
        }

        [TestMethod]
        public void Read_Number_ReturnsRawNumberText()
        {
            Assert.AreEqual("42", Read("42"));
        }

        [TestMethod]
        public void Read_Object_ReturnsRawObjectJson()
        {
            Assert.AreEqual("{\"type\":\"text\",\"text\":\"x\"}", Read("{\"type\":\"text\",\"text\":\"x\"}"));
        }

        [TestMethod]
        public void Deserialize_ArrayContentProperty_FlattensToText()
        {
            TestDto sut = JsonSerializer.Deserialize<TestDto>("{\"Content\":[{\"type\":\"text\",\"text\":\"hello\"},{\"type\":\"text\",\"text\":\" world\"}]}");

            Assert.IsNotNull(sut);
            Assert.AreEqual("hello world", sut.Content);
        }

        [TestMethod]
        public void Serialize_String_WritesJsonString()
        {
            Assert.AreEqual("\"hello\"", Write("hello"));
        }

        [TestMethod]
        public void Serialize_Null_WritesNull()
        {
            Assert.AreEqual("null", Write(null));
        }

        private static string Read(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            Utf8JsonReader reader = new(bytes);
            Assert.IsTrue(reader.Read(), "Expected at least one JSON token.");
            return new MessageContentJsonConverter().Read(ref reader, typeof(string), null);
        }

        private static string Write(string value)
        {
            using MemoryStream stream = new();
            using (Utf8JsonWriter writer = new(stream))
            {
                new MessageContentJsonConverter().Write(writer, value, null);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private sealed class TestDto
        {
            [JsonConverter(typeof(MessageContentJsonConverter))]
            public string Content { get; set; }
        }
    }
}
