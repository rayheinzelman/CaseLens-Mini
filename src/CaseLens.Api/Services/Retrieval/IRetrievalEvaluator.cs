namespace CaseLens.Api.Services.Retrieval;

public interface IRetrievalEvaluator
{
    Task<RetrievalEvaluationReport> EvaluateAsync(
        CancellationToken cancellationToken = default);
}
