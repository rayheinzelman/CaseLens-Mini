using System.Text;

namespace CaseLens.Api.Services.Answers;

public sealed class AnswerPromptBuilder : IAnswerPromptBuilder
{
    public string Build(
        string question,
        IReadOnlyList<AnswerEvidence> evidence)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are CaseLens, a legal research aid, not a lawyer.");
        builder.AppendLine("Answer only from the EVIDENCE supplied below.");
        builder.AppendLine("Do not use general legal knowledge.");
        builder.AppendLine("Do not invent case names, holdings, quotations, pages, or evidence IDs.");
        builder.AppendLine("Cite only the exact evidence IDs shown below.");
        builder.AppendLine("If the evidence does not support an answer, set insufficientEvidence to true and say so plainly.");
        builder.AppendLine();
        builder.AppendLine($"QUESTION: {question}");
        builder.AppendLine();
        builder.AppendLine("EVIDENCE:");

        foreach (var item in evidence)
        {
            var result = item.RetrievalResult;
            builder.AppendLine($"[{item.EvidenceId}]");
            builder.AppendLine($"Document: {result.DocumentTitle}");
            builder.AppendLine($"Citation: {result.Citation}");
            builder.AppendLine($"Page: {result.PageNumber}");
            builder.AppendLine($"Chunk: {result.ChunkId}");
            builder.AppendLine("Passage:");
            builder.AppendLine(result.Content);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
