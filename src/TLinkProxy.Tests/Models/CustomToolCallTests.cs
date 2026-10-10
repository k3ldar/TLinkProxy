using TLinkProxy.Base;
using TLinkProxy.Kernel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TLinkProxy.Tests.Models
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class CustomToolCallTests
    {
        [TestMethod]
        public void Deserialize_CustomToolCall_PreservesNameAndInput()
        {
            string json = "{\"id\":\"custom_call_1\",\"type\":\"custom\",\"custom\":{\"name\":\"apply_patch\",\"input\":\"*** Begin Patch\\n*** End Patch\\n\"}}";

            ToolCall sut = JsonSerializer.Deserialize<ToolCall>(json, Constants.JsonRequestOptions);

            Assert.IsNotNull(sut);
            Assert.AreEqual("custom_call_1", sut.Id);
            Assert.AreEqual("custom", sut.Type);
            Assert.IsNull(sut.Function);
            Assert.AreEqual("apply_patch", sut.Name);
            Assert.AreEqual("*** Begin Patch\n*** End Patch\n", sut.Arguments);
        }

        [TestMethod]
        public void Serialize_CustomToolCall_WritesCustomPayload()
        {
            ToolCall sut = new()
            {
                Id = "custom_call_1",
                Type = "custom",
                Custom = new CustomToolCall
                {
                    Name = "apply_patch",
                    Input = "patch"
                }
            };

            string result = JsonSerializer.Serialize(sut, Constants.JsonRequestOptions);

            StringAssert.Contains(result, "\"custom\":{\"name\":\"apply_patch\",\"input\":\"patch\"}");
            Assert.DoesNotContain(result, "\"function\"");
        }
    }
}
