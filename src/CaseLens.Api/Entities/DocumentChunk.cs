namespace CaseLens.Api.Entities;

public sealed class DocumentChunk
{
    public int Id { get; set; }

    public int LegalDocumentId { get; set; }

    public LegalDocument LegalDocument { get; set; } = null!;

    public int PageNumber { get; set; }

    public int ChunkIndex { get; set; }

    public required string Content { get; set; }

    public float[]? Embedding { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}