namespace CaseLens.Api.Services.Retrieval;

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
