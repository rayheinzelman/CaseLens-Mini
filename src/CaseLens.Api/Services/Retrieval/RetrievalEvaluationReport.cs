namespace CaseLens.Api.Services.Retrieval;

public sealed record RetrievalEvaluationResult(
    string Question,
    string ExpectedCitation,
    IReadOnlyList<string> RetrievedCitations,
    bool Passed);

public sealed record RetrievalEvaluationReport(
    IReadOnlyList<RetrievalEvaluationResult> Results)
{
    public bool Passed =>
        Results.Count > 0 &&
        Results.All(result => result.Passed);
}
