using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Kernel;
using System.Text;

namespace TLinkProxy.Costing
{
    public class CostEstimator : ICostEstimator
    {
        public double Estimate(OpenAIModel request, IAiModelConfig modelConfig)
        {
            if (modelConfig == null)
            {
                return 0;
            }

            double inputCostPer1k = modelConfig.CostPer1mInput;
            double outputCostPer1k = modelConfig.CostPer1mOutput;

            if (inputCostPer1k <= 0 && outputCostPer1k <= 0)
            {
                return 0;
            }

            double inputTokens = EstimateInputTokens(request);
            double outputTokens = EstimateOutputTokens(request);

            return (inputTokens / Constants.CostPerThousandDivisor * inputCostPer1k) + (outputTokens / Constants.CostPerThousandDivisor * outputCostPer1k);
        }

        public int EstimateInputTokens(OpenAIModel request)
        {
            long charCount = CountInputCharacters(request);
            return (int)Math.Ceiling((double)charCount / Constants.CharsPerToken);
        }

        private static double EstimateOutputTokens(OpenAIModel request)
        {
            if (request?.MaxCompletionTokens > 0)
            {
                return request.MaxCompletionTokens;
            }

            return Constants.DefaultOutputTokenEstimate; // conservative default
        }

        private static long CountInputCharacters(OpenAIModel request)
        {
            if (request?.Messages == null)
            {
                return 0;
            }

            long charCount = 0;

            foreach (Message message in request.Messages)
            {
                if (message == null)
                {
                    continue;
                }

                charCount += message.Content?.Length ?? 0;
                charCount += message.Role?.Length ?? 0;

                if (message.ToolCalls != null)
                {
                    foreach (ToolCall call in message.ToolCalls)
                    {
                        charCount += call?.Arguments?.Length ?? 0;
                        charCount += call?.Name?.Length ?? 0;
                    }
                }
            }

            if (request.Tools != null)
            {
                foreach (Tool tool in request.Tools)
                {
                    if (tool?.Function == null)
                    {
                        continue;
                    }

                    charCount += tool.Function.Name?.Length ?? 0;
                    charCount += tool.Function.Description?.Length ?? 0;

                    if (tool.Function.Parameters != null)
                    {
                        try
                        {
                            charCount += Encoding.UTF8.GetByteCount(
                                System.Text.Json.JsonSerializer.Serialize(tool.Function.Parameters, Constants.JsonRequestOptions));
                        }
                        catch
                        {
                            // ignore serialization cost
                        }
                    }
                }
            }

            return charCount;
        }
    }
}
