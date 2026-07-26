using CaseLens.Api.Services.Ingestion;
using CaseLens.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Tests.Services.Ingestion;

[Collection("PostgreSQL")]
public sealed class DocumentIngestionServiceTests
{
    private readonly PostgresDatabaseFixture _database;

    public DocumentIngestionServiceTests(
        PostgresDatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task IngestAsync_OrdersPagesBeforeAssigningChunkIndexes()
    {
        await _database.ResetAsync();

        var pdfPath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(
                pdfPath,
                "Fake PDF bytes for the ordering test.");

            var extractor = new FakePdfTextExtractor(
            [
                new ExtractedPage(
                    2,
                    "Content from the second page."),

                new ExtractedPage(
                    1,
                    "Content from the first page.")
            ]);

            await using var dbContext =
                _database.CreateDbContext();

            var service = new DocumentIngestionService(
                dbContext,
                extractor,
                new DeterministicTextChunker());

            var source = new OpinionSource(
                pdfPath,
                "Test Opinion",
                "123 Test Reporter 456");

            var result = await service.IngestAsync(source);

            var chunks = await dbContext.DocumentChunks
                .AsNoTracking()
                .OrderBy(chunk => chunk.ChunkIndex)
                .ToListAsync();

            Assert.False(result.WasSkipped);
            Assert.Equal(2, result.PageCount);
            Assert.Equal(2, result.ChunkCount);
            Assert.Equal(2, chunks.Count);

            Assert.Equal(1, chunks[0].PageNumber);
            Assert.Equal(0, chunks[0].ChunkIndex);
            Assert.Equal(
                "Content from the first page.",
                chunks[0].Content);

            Assert.Equal(2, chunks[1].PageNumber);
            Assert.Equal(1, chunks[1].ChunkIndex);
            Assert.Equal(
                "Content from the second page.",
                chunks[1].Content);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }

    [Fact]
    public async Task IngestAsync_SameFileTwice_DoesNotDuplicateRecords()
    {
        await _database.ResetAsync();

        var pdfPath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(
                pdfPath,
                "Fake PDF bytes for the idempotency test.");

            var extractor = new FakePdfTextExtractor(
            [
                new ExtractedPage(
                    1,
                    "Content from the first page."),

                new ExtractedPage(
                    2,
                    "Content from the second page.")
            ]);

            await using var dbContext =
                _database.CreateDbContext();

            var service = new DocumentIngestionService(
                dbContext,
                extractor,
                new DeterministicTextChunker());

            var source = new OpinionSource(
                pdfPath,
                "Test Opinion",
                "123 Test Reporter 456");

            var firstResult =
                await service.IngestAsync(source);

            var secondResult =
                await service.IngestAsync(source);

            var documentCount =
                await dbContext.LegalDocuments.CountAsync();

            var chunkCount =
                await dbContext.DocumentChunks.CountAsync();

            Assert.False(firstResult.WasSkipped);
            Assert.True(secondResult.WasSkipped);

            Assert.Equal(1, documentCount);
            Assert.Equal(2, chunkCount);

            Assert.Equal(
                firstResult.ChunkCount,
                secondResult.ChunkCount);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }
}
