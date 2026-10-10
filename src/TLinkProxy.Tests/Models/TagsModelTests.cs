using TLinkProxy.Base.Models;
using System.Diagnostics.CodeAnalysis;

namespace TLinkProxy.Tests.Models
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TagsModelTests
    {
        [TestMethod]
        public void TestTagsModelInitialization()
        {
            // Arrange
            TagsModel tagsModel = new()
            {
                models = [new TagModel { name = "TestTag" }]
            };

            // Act & Assert
            Assert.IsNotNull(tagsModel.models);
            Assert.HasCount(1, tagsModel.models);
            Assert.AreEqual("TestTag", tagsModel.models[0].name);
        }
    }
}
