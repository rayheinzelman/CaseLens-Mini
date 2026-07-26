namespace CaseLens.Api.Services.Retrieval;

public sealed class RetrievalEvaluator : IRetrievalEvaluator
{
    private const int EvaluationTopK = 3;

    private static readonly EvaluationCase[] Cases =
    [
        new(
            "When may police stop and frisk a person " +
            "based on reasonable suspicion?",
            "392 U.S. 1 (1968)"),
        new(
            "What standard governs an excessive force claim " +
            "arising during an arrest?",
            "490 U.S. 386 (1989)"),
        new(
            "When may police search a vehicle incident " +
            "to arrest of an occupant?",
            "556 U. S. 332 (2009)")
    ];

    private readonly IRetrievalService _retrievalService;

    public RetrievalEvaluator(
        IRetrievalService retrievalService)
    {
        _retrievalService = retrievalService;
    }

    public async Task<RetrievalEvaluationReport> EvaluateAsync(
        CancellationToken cancellationToken = default)
    {
        var results =
            new List<RetrievalEvaluationResult>(Cases.Length);

        foreach (var evaluationCase in Cases)
        {
            var retrieved = await _retrievalService.RetrieveAsync(
                evaluationCase.Question,
                EvaluationTopK,
                cancellationToken);

            var citations = retrieved
                .Select(result => result.Citation)
                .ToArray();

            results.Add(new RetrievalEvaluationResult(
                evaluationCase.Question,
                evaluationCase.ExpectedCitation,
                citations,
                citations.Contains(
                    evaluationCase.ExpectedCitation,
                    StringComparer.Ordinal)));
        }

        return new RetrievalEvaluationReport(results);
    }

    private sealed record EvaluationCase(
        string Question,
        string ExpectedCitation);
}
