using CaseLens.Api.Data;
using CaseLens.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Services.Embeddings;

public sealed class ChunkEmbeddingBackfillService
    : IChunkEmbeddingBackfillService
{
    private const int BatchSize = 64;

    private readonly CaseLensDbContext _dbContext;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public ChunkEmbeddingBackfillService(
        CaseLensDbContext dbContext,
        IEmbeddingGenerator embeddingGenerator)
    {
        _dbContext = dbContext;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<EmbeddingBackfillResult> PopulateMissingAsync(
        CancellationToken cancellationToken = default)
    {
        var chunks = await _dbContext.DocumentChunks
            .OrderBy(chunk => chunk.LegalDocumentId)
            .ThenBy(chunk => chunk.ChunkIndex)
            .ToListAsync(cancellationToken);

        ValidateExistingEmbeddings(chunks);

        var missingChunks = chunks
            .Where(chunk =>
                chunk.Embedding is null ||
                chunk.Embedding.Length == 0)
            .ToList();

        var existingEmbeddingCount =
            chunks.Count - missingChunks.Count;

        var generatedEmbeddingCount = 0;

        foreach (var batch in missingChunks.Chunk(BatchSize))
        {
            var inputs = batch
                .Select(chunk => chunk.Content)
                .ToList();

            var embeddings =
                await _embeddingGenerator.GenerateAsync(
                    inputs,
                    cancellationToken);

            if (embeddings.Count != batch.Length)
            {
                throw new InvalidOperationException(
                    "The embedding generator returned a different " +
                    "number of vectors than requested.");
            }

            for (var index = 0; index < batch.Length; index++)
            {
                var embedding = embeddings[index];

                if (embedding.Length !=
                    _embeddingGenerator.Dimensions)
                {
                    throw new InvalidOperationException(
                        "The embedding generator returned an " +
                        "inconsistent vector length.");
                }

                batch[index].Embedding = embedding;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            generatedEmbeddingCount += batch.Length;
        }

        return new EmbeddingBackfillResult(
            chunks.Count,
            existingEmbeddingCount,
            generatedEmbeddingCount,
            _embeddingGenerator.Dimensions);
    }

    private void ValidateExistingEmbeddings(
        IReadOnlyList<DocumentChunk> chunks)
    {
        var inconsistentChunk = chunks.FirstOrDefault(chunk =>
            chunk.Embedding is { Length: > 0 } &&
            chunk.Embedding.Length !=
                _embeddingGenerator.Dimensions);

        if (inconsistentChunk is not null)
        {
            throw new InvalidOperationException(
                $"Document chunk {inconsistentChunk.Id} has an " +
                $"embedding with " +
                $"{inconsistentChunk.Embedding!.Length} dimensions; " +
                $"{_embeddingGenerator.Dimensions} were configured. " +
                "Refusing to mix inconsistent stored embeddings.");
        }
    }
}
