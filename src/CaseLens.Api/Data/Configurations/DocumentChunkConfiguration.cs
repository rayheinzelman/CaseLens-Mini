using CaseLens.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaseLens.Api.Data.Configurations;

public sealed class DocumentChunkConfiguration
    : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Id)
            .HasColumnName("id");

        builder.Property(chunk => chunk.LegalDocumentId)
            .HasColumnName("legal_document_id")
            .IsRequired();

        builder.Property(chunk => chunk.PageNumber)
            .HasColumnName("page_number")
            .IsRequired();

        builder.Property(chunk => chunk.ChunkIndex)
            .HasColumnName("chunk_index")
            .IsRequired();

        builder.Property(chunk => chunk.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(chunk => chunk.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("real[]")
            .IsRequired();

        builder.Property(chunk => chunk.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(chunk => new
        {
            chunk.LegalDocumentId,
            chunk.ChunkIndex
        }).IsUnique();
    }
}