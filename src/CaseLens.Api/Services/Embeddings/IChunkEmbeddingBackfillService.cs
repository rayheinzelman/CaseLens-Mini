namespace CaseLens.Api.Services.Embeddings;

public interface IChunkEmbeddingBackfillService
{
    Task<EmbeddingBackfillResult> PopulateMissingAsync(
        CancellationToken cancellationToken = default);
}
