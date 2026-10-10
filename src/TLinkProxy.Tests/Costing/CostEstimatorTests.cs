using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Costing;
using TLinkProxy.Kernel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace TLinkProxy.Tests.Costing
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public sealed class CostEstimatorTests
    {
        [TestMethod]
        public void Estimate_NullBackend_ReturnsZero()
        {
            CostEstimator sut = new();

            double result = sut.Estimate(new OpenAIModel(), null);

            Assert.AreEqual(0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_NoCostsConfigured_ReturnsZero()
        {
            CostEstimator sut = new();
            AiModelConfig backend = new();

            double result = sut.Estimate(new OpenAIModel(), backend);

            Assert.AreEqual(0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_ZeroCosts_ReturnsZero()
        {
            CostEstimator sut = new();
            AiModelConfig backend = new() { CostPer1mInput = 0, CostPer1mOutput = 0 };

            double result = sut.Estimate(new OpenAIModel(), backend);

            Assert.AreEqual(0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_NegativeCosts_ReturnsZero()
        {
            CostEstimator sut = new();
            AiModelConfig backend = new() { CostPer1mInput = -1, CostPer1mOutput = -2 };

            double result = sut.Estimate(new OpenAIModel(), backend);

            Assert.AreEqual(0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_InputCostOnly_ReturnsInputCost()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [new Message { Content = "abcd" }],
                MaxCompletionTokens = 0
            };
            AiModelConfig backend = new() { CostPer1mInput = 1000, CostPer1mOutput = 0 };

            double result = sut.Estimate(request, backend);

            Assert.AreEqual(1.0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_OutputCostOnly_ReturnsOutputCost()
        {
            CostEstimator sut = new();
            AiModelConfig backend = new() { CostPer1mInput = 0, CostPer1mOutput = 1000 };

            double result = sut.Estimate(null, backend);

            Assert.AreEqual(1024.0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_BothCosts_ReturnsSum()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [new Message { Role = "user", Content = "abcd" }],
                MaxCompletionTokens = 2000
            };
            AiModelConfig backend = new() { CostPer1mInput = 1000, CostPer1mOutput = 500 };

            double result = sut.Estimate(request, backend);

            Assert.AreEqual(1002.0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_MaxCompletionTokensZero_UsesDefaultOutputEstimate()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [new Message { Content = "abcd" }],
                MaxCompletionTokens = 0
            };
            AiModelConfig backend = new() { CostPer1mInput = 1000, CostPer1mOutput = 1000 };

            double result = sut.Estimate(request, backend);

            Assert.AreEqual(1025.0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_NegativeMaxCompletionTokens_UsesDefaultOutputEstimate()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [new Message { Content = "abcd" }],
                MaxCompletionTokens = -5
            };
            AiModelConfig backend = new() { CostPer1mInput = 1000, CostPer1mOutput = 1000 };

            double result = sut.Estimate(request, backend);

            Assert.AreEqual(1025.0, result, 0.0001);
        }

        [TestMethod]
        public void Estimate_NullRequest_UsesDefaultOutputAndZeroInput()
        {
            CostEstimator sut = new();
            AiModelConfig backend = new() { CostPer1mInput = 1000, CostPer1mOutput = 1000 };

            double result = sut.Estimate(null, backend);

            Assert.AreEqual(1024.0, result, 0.0001);
        }

        [TestMethod]
        public void EstimateInputTokens_NullRequest_ReturnsZero()
        {
            CostEstimator sut = new();

            int result = sut.EstimateInputTokens(null);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_NullMessages_ReturnsZero()
        {
            CostEstimator sut = new();
            OpenAIModel request = new();

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_EmptyMessages_ReturnsZero()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_EmptyContent_ReturnsZero()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [new Message { Content = "" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_FourChars_ReturnsOne()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [new Message { Content = "abcd" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void EstimateInputTokens_FiveChars_RoundsUpToTwo()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [new Message { Content = "hello" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void EstimateInputTokens_OneChar_ReturnsOne()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [new Message { Content = "a" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void EstimateInputTokens_RoleAndContent_AreSummed()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [new Message { Role = "user", Content = "hi" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void EstimateInputTokens_NullMessage_IsSkipped()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [null, new Message { Content = "abcd" }] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void EstimateInputTokens_ToolCalls_AreCounted()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages =
                [
                    new Message
                    {
                        ToolCalls =
                        [
                            new ToolCall { Function = new FunctionCall { Name = "func", Arguments = "arg" } }
                        ]
                    }
                ]
            };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void EstimateInputTokens_ToolCallNullFunction_IsIgnored()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [new Message { ToolCalls = [new ToolCall()] }]
            };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_Tools_CountNameAndDescription()
        {
            CostEstimator sut = new();
            OpenAIModel request = new()
            {
                Messages = [],
                Tools = [new Tool { Function = new Function { Name = "fname", Description = "desc" } }]
            };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void EstimateInputTokens_ToolNullFunction_IsIgnored()
        {
            CostEstimator sut = new();
            OpenAIModel request = new() { Messages = [], Tools = [new Tool()] };

            int result = sut.EstimateInputTokens(request);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void EstimateInputTokens_ToolsWithParameters_CountsSerializedLength()
        {
            CostEstimator sut = new();
            Parameters parameters = new();
            OpenAIModel request = new()
            {
                Messages = [],
                Tools =
                [
                    new Tool
                    {
                        Function = new Function { Name = "n", Description = "d", Parameters = parameters }
                    }
                ]
            };

            int result = sut.EstimateInputTokens(request);

            int expectedByteCount = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(parameters, Constants.JsonRequestOptions));
            int expectedTokens = (int)Math.Ceiling((2 + expectedByteCount) / 4.0);
            Assert.AreEqual(expectedTokens, result);
        }

        [TestMethod]
        public void EstimateInputTokens_ToolsWithParameters_ExceedsWithoutParameters()
        {
            CostEstimator sut = new();
            OpenAIModel withParameters = new()
            {
                Messages = [],
                Tools = [new Tool { Function = new Function { Name = "n", Parameters = new Parameters() } }]
            };
            OpenAIModel withoutParameters = new()
            {
                Messages = [],
                Tools = [new Tool { Function = new Function { Name = "n" } }]
            };

            int withResult = sut.EstimateInputTokens(withParameters);
            int withoutResult = sut.EstimateInputTokens(withoutParameters);

            Assert.IsGreaterThan(withoutResult, withResult);
        }
    }
}
