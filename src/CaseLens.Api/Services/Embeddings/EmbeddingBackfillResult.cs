namespace CaseLens.Api.Services.Embeddings;

public sealed record EmbeddingBackfillResult(
    int TotalChunkCount,
    int ExistingEmbeddingCount,
    int GeneratedEmbeddingCount,
    int Dimensions);
