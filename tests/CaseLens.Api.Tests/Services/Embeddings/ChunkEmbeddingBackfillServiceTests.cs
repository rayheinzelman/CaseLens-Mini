using CaseLens.Api.Entities;
using CaseLens.Api.Services.Embeddings;
using CaseLens.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Tests.Services.Embeddings;

[Collection("PostgreSQL")]
public sealed class ChunkEmbeddingBackfillServiceTests
{
    private readonly PostgresDatabaseFixture _database;

    public ChunkEmbeddingBackfillServiceTests(
        PostgresDatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task PopulateMissingAsync_GeneratesOnlyMissingVectors()
    {
        await _database.ResetAsync();

        await using var dbContext = _database.CreateDbContext();

        dbContext.LegalDocuments.Add(CreateDocument(
            new DocumentChunk
            {
                PageNumber = 1,
                ChunkIndex = 0,
                Content = "already embedded",
                Embedding = [0.1f, 0.2f, 0.3f],
                CreatedAt = DateTimeOffset.UtcNow
            },
            new DocumentChunk
            {
                PageNumber = 1,
                ChunkIndex = 1,
                Content = "missing one",
                Embedding = null,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new DocumentChunk
            {
                PageNumber = 2,
                ChunkIndex = 2,
                Content = "missing two",
                Embedding = [],
                CreatedAt = DateTimeOffset.UtcNow
            }));

        await dbContext.SaveChangesAsync();

        var generator = new FakeEmbeddingGenerator(
            dimensions: 3);

        var service = new ChunkEmbeddingBackfillService(
            dbContext,
            generator);

        var result = await service.PopulateMissingAsync();

        var storedChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync();

        Assert.Equal(3, result.TotalChunkCount);
        Assert.Equal(1, result.ExistingEmbeddingCount);
        Assert.Equal(2, result.GeneratedEmbeddingCount);
        Assert.Equal(3, result.Dimensions);

        Assert.Single(generator.Requests);
        Assert.Equal(
            ["missing one", "missing two"],
            generator.Requests[0]);

        Assert.All(
            storedChunks,
            chunk => Assert.Equal(
                3,
                chunk.Embedding?.Length));
    }

    [Fact]
    public async Task PopulateMissingAsync_RejectsInconsistentStoredVector()
    {
        await _database.ResetAsync();

        await using var dbContext = _database.CreateDbContext();

        dbContext.LegalDocuments.Add(CreateDocument(
            new DocumentChunk
            {
                PageNumber = 1,
                ChunkIndex = 0,
                Content = "wrong dimensions",
                Embedding = [0.1f, 0.2f],
                CreatedAt = DateTimeOffset.UtcNow
            }));

        await dbContext.SaveChangesAsync();

        var generator = new FakeEmbeddingGenerator(
            dimensions: 3);

        var service = new ChunkEmbeddingBackfillService(
            dbContext,
            generator);

        var exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() =>
                service.PopulateMissingAsync());

        Assert.Contains(
            "Refusing to mix inconsistent stored embeddings",
            exception.Message);

        Assert.Empty(generator.Requests);
    }

    private static LegalDocument CreateDocument(
        params DocumentChunk[] chunks)
    {
        var document = new LegalDocument
        {
            Title = "Test Opinion",
            Citation = "123 Test Reporter 456",
            SourceFileName =
                $"{Guid.NewGuid():N}.pdf",
            ContentHash =
                new string('a', 64),
            PageCount = 2,
            ImportedAt = DateTimeOffset.UtcNow
        };

        foreach (var chunk in chunks)
        {
            document.Chunks.Add(chunk);
        }

        return document;
    }
}
