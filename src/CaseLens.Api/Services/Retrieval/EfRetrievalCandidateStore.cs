using CaseLens.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Services.Retrieval;

public sealed class EfRetrievalCandidateStore
    : IRetrievalCandidateStore
{
    private readonly CaseLensDbContext _dbContext;

    public EfRetrievalCandidateStore(
        CaseLensDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RetrievalCandidate>>
        LoadEmbeddedAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            .OrderBy(chunk => chunk.LegalDocumentId)
            .ThenBy(chunk => chunk.ChunkIndex)
            .Select(chunk => new RetrievalCandidate(
                chunk.Id,
                chunk.LegalDocumentId,
                chunk.LegalDocument.Title,
                chunk.LegalDocument.Citation,
                chunk.PageNumber,
                chunk.ChunkIndex,
                chunk.Content,
                chunk.Embedding!))
            .ToListAsync(cancellationToken);
    }
}
