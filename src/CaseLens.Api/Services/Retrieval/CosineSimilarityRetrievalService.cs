using CaseLens.Api.Services.Embeddings;

namespace CaseLens.Api.Services.Retrieval;

public sealed class CosineSimilarityRetrievalService
    : IRetrievalService
{
    private const int MaximumTopK = 20;

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IRetrievalCandidateStore _candidateStore;

    public CosineSimilarityRetrievalService(
        IEmbeddingGenerator embeddingGenerator,
        IRetrievalCandidateStore candidateStore)
    {
        _embeddingGenerator = embeddingGenerator;
        _candidateStore = candidateStore;
    }

    public async Task<IReadOnlyList<RetrievalResult>>
        RetrieveAsync(
            string question,
            int topK = 5,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        if (topK is < 1 or > MaximumTopK)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                $"TopK must be between 1 and {MaximumTopK}.");
        }

        var generated = await _embeddingGenerator.GenerateAsync(
            [question.Trim()],
            cancellationToken);

        if (generated.Count != 1)
        {
            throw new InvalidOperationException(
                "Embedding generator did not return one query vector.");
        }

        var queryEmbedding = generated[0];

        if (queryEmbedding.Length != _embeddingGenerator.Dimensions)
        {
            throw new InvalidOperationException(
                "Query embedding dimensions do not match configuration.");
        }

        var candidates = await _candidateStore.LoadEmbeddedAsync(
            cancellationToken);

        foreach (var candidate in candidates)
        {
            if (candidate.Embedding.Length != queryEmbedding.Length)
            {
                throw new InvalidOperationException(
                    $"Chunk {candidate.ChunkId} has " +
                    $"{candidate.Embedding.Length} dimensions; " +
                    $"expected {queryEmbedding.Length}.");
            }
        }

        return candidates
            .Select(candidate => new RetrievalResult(
                candidate.ChunkId,
                candidate.LegalDocumentId,
                candidate.DocumentTitle,
                candidate.Citation,
                candidate.PageNumber,
                candidate.ChunkIndex,
                candidate.Content,
                CosineSimilarity.Calculate(
                    queryEmbedding,
                    candidate.Embedding)))
            .OrderByDescending(result => result.Similarity)
            .ThenBy(result => result.LegalDocumentId)
            .ThenBy(result => result.ChunkIndex)
            .Take(topK)
            .ToArray();
    }
}
