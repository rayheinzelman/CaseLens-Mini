using System.Security.Cryptography;
using CaseLens.Api.Data;
using CaseLens.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Services.Ingestion;

public sealed class DocumentIngestionService
    : IDocumentIngestionService
{
    private readonly CaseLensDbContext _dbContext;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextChunker _textChunker;

    public DocumentIngestionService(
        CaseLensDbContext dbContext,
        IPdfTextExtractor pdfTextExtractor,
        ITextChunker textChunker)
    {
        _dbContext = dbContext;
        _pdfTextExtractor = pdfTextExtractor;
        _textChunker = textChunker;
    }

    public async Task<IngestionResult> IngestAsync(
        OpinionSource source,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(source.FilePath))
        {
            throw new FileNotFoundException(
                "The opinion PDF was not found.",
                source.FilePath);
        }

        var sourceFileName = Path.GetFileName(source.FilePath);

        var contentHash = await ComputeHashAsync(
            source.FilePath,
            cancellationToken);

        var existingDocument = await _dbContext.LegalDocuments
            .Include(document => document.Chunks)
            .SingleOrDefaultAsync(
                document =>
                    document.SourceFileName == sourceFileName,
                cancellationToken);

        if (existingDocument?.ContentHash == contentHash)
        {
            return new IngestionResult(
                sourceFileName,
                existingDocument.PageCount,
                existingDocument.Chunks.Count,
                WasSkipped: true);
        }

        var pages = _pdfTextExtractor
            .ExtractPages(source.FilePath)
            .OrderBy(page => page.PageNumber)
            .ToList();

        var importedAt = DateTimeOffset.UtcNow;

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        LegalDocument document;

        if (existingDocument is null)
        {
            document = new LegalDocument
            {
                Title = source.Title,
                Citation = source.Citation,
                SourceFileName = sourceFileName,
                ContentHash = contentHash,
                PageCount = pages.Count,
                ImportedAt = importedAt
            };

            _dbContext.LegalDocuments.Add(document);
        }
        else
        {
            document = existingDocument;

            var existingChunks = document.Chunks.ToList();

            _dbContext.DocumentChunks.RemoveRange(existingChunks);
            document.Chunks.Clear();

            document.Title = source.Title;
            document.Citation = source.Citation;
            document.ContentHash = contentHash;
            document.PageCount = pages.Count;
            document.ImportedAt = importedAt;
        }

        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var pageChunks = _textChunker.Chunk(page.Content);

            foreach (var chunkContent in pageChunks)
            {
                document.Chunks.Add(new DocumentChunk
                {
                    PageNumber = page.PageNumber,
                    ChunkIndex = chunkIndex,
                    Content = chunkContent,
                    Embedding = null,
                    CreatedAt = importedAt
                });

                chunkIndex++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IngestionResult(
            sourceFileName,
            pages.Count,
            chunkIndex,
            WasSkipped: false);
    }

    private static async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);

        var hash = await SHA256.HashDataAsync(
            stream,
            cancellationToken);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}