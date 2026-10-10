using TLinkProxy.Abstractions;
using TLinkProxy.Base.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace TLinkProxy.Tests.Configuration
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class AiModelConfigTests
    {
        [TestMethod]
        public void IsOnDemand_LlamacppWithPositiveTimeout_ReturnsTrue()
        {
            AiModelConfig sut = new()
            {
                Kind = ModelKind.LlamaCpp,
                Details = new LlamaCppConfig { TimeoutSeconds = 30 }
            };

            Assert.IsTrue(sut.IsOnDemand);
        }

        [TestMethod]
        public void IsOnDemand_LlamacppWithZeroTimeout_ReturnsFalse()
        {
            AiModelConfig sut = new()
            {
                Kind = ModelKind.LlamaCpp,
                Details = new LlamaCppConfig { TimeoutSeconds = 0 }
            };

            Assert.IsFalse(sut.IsOnDemand);
        }

        [TestMethod]
        public void IsOnDemand_LlamacppWithNegativeTimeout_ReturnsFalse()
        {
            AiModelConfig sut = new()
            {
                Kind = ModelKind.LlamaCpp,
                Details = new LlamaCppConfig { TimeoutSeconds = -5 }
            };

            Assert.IsFalse(sut.IsOnDemand);
        }

        [TestMethod]
        public void IsOnDemand_LlamacppWithoutDetails_ReturnsFalse()
        {
            AiModelConfig sut = new()
            {
                Kind = ModelKind.LlamaCpp
            };

            Assert.IsFalse(sut.IsOnDemand);
        }

        [TestMethod]
        public void IsOnDemand_NonLlamacppWithPositiveTimeout_ReturnsFalse()
        {
            AiModelConfig sut = new()
            {
                Kind = ModelKind.Ollama,
                Details = new LlamaCppConfig { TimeoutSeconds = 30 }
            };

            Assert.IsFalse(sut.IsOnDemand);
        }
    }
}
