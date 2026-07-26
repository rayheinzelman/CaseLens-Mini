namespace CaseLens.Api.Services.Retrieval;

public interface IRetrievalCandidateStore
{
    Task<IReadOnlyList<RetrievalCandidate>> LoadEmbeddedAsync(
        CancellationToken cancellationToken = default);
}
