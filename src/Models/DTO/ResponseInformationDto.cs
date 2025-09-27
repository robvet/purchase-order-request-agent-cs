namespace SingleAgent.Models.DTO
{
    public class ResponseInformationDto
    {
        public ResponseInformationDto(int inputTokens, int outputTokens, int reasoningTokens)
        {
            InputTokens = inputTokens;
            OutputTokens = outputTokens;
            ReasoningTokens = reasoningTokens;
        }

        public int InputTokens { get;  }
        public int OutputTokens { get;  }
        public int ReasoningTokens { get; }
    }
}
