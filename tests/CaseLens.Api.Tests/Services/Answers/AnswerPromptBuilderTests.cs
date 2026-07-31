using CaseLens.Api.Services.Answers;
using CaseLens.Api.Services.Retrieval;

namespace CaseLens.Api.Tests.Services.Answers;

public sealed class AnswerPromptBuilderTests
{
    [Fact]
    public void Build_LabelsOnlyApplicationSuppliedEvidence()
    {
        var evidence = new[]
        {
            new AnswerEvidence(
                "C1",
                new RetrievalResult(
                    42,
                    1,
                    "TERRY v. OHIO.",
                    "392 U.S. 1 (1968)",
                    9,
                    3,
                    "Specific and articulable facts.",
                    0.88))
        };

        var prompt = new AnswerPromptBuilder().Build(
            "When is a stop justified?",
            evidence);

        Assert.Contains("[C1]", prompt);
        Assert.Contains("Specific and articulable facts.", prompt);
        Assert.Contains("Answer only from the EVIDENCE", prompt);
        Assert.Contains("not legal advice", prompt);
        Assert.DoesNotContain("C2", prompt);
    }
}
