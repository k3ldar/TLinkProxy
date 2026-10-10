using TLinkProxy.Base.Models;
using System.Diagnostics.CodeAnalysis;

namespace TLinkProxy.Tests.Models
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ModelTokenStatsTests
    {
        [TestMethod]
        public void ModelTokenStats_WithPositiveValues_TokensSavedCorrect()
        {
            // Arrange
            var sut = new ModelTokenStats
            {
                OriginalTokens = 1000,
                OptimizedTokens = 800
            };

            // Assert
            Assert.AreEqual(200, sut.TokensSaved);
        }

        [TestMethod]
        public void ModelTokenStats_OriginalEqualsOptimized_TokensSavedZero()
        {
            // Arrange
            var sut = new ModelTokenStats
            {
                OriginalTokens = 500,
                OptimizedTokens = 500
            };

            // Assert
            Assert.AreEqual(0, sut.TokensSaved);
        }

        [TestMethod]
        public void ModelTokenStats_OriginalLessThanOptimized_TokensSavedNegative()
        {
            // Arrange
            var sut = new ModelTokenStats
            {
                OriginalTokens = 300,
                OptimizedTokens = 400
            };

            // Assert
            Assert.AreEqual(-100, sut.TokensSaved);
        }

        [TestMethod]
        public void ModelTokenStats_ZeroRequestCount_RequestCountIsZero()
        {
            // Arrange
            var sut = new ModelTokenStats();

            // Assert
            Assert.AreEqual(0, sut.RequestCount);
        }

        [TestMethod]
        public void ModelTokenStats_SetProperties_ValuesAreAssignedCorrectly()
        {
            // Arrange
            var sut = new ModelTokenStats
            {
                RequestCount = 10,
                OriginalTokens = 2000,
                OptimizedTokens = 1500
            };

            // Assert
            Assert.AreEqual(10, sut.RequestCount);
            Assert.AreEqual(2000, sut.OriginalTokens);
            Assert.AreEqual(1500, sut.OptimizedTokens);
            Assert.AreEqual(500, sut.TokensSaved);
        }
    }
}