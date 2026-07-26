namespace CaseLens.Api.Entities;

public sealed class LegalDocument
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Citation { get; set; }

    public required string SourceFileName { get; set; }

    public required string ContentHash { get; set; }

    public int PageCount { get; set; }

    public DateTimeOffset ImportedAt { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } =
        new List<DocumentChunk>();
}