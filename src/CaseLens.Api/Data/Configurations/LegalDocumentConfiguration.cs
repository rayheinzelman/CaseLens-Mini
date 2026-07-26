using CaseLens.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaseLens.Api.Data.Configurations;

public sealed class LegalDocumentConfiguration
    : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> builder)
    {
        builder.ToTable("legal_documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasColumnName("id");

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.Citation)
            .HasColumnName("citation")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(document => document.SourceFileName)
            .HasColumnName("source_file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(document => document.PageCount)
            .HasColumnName("page_count")
            .IsRequired();

        builder.Property(document => document.ImportedAt)
            .HasColumnName("imported_at")
            .IsRequired();

        builder.HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.LegalDocument)
            .HasForeignKey(chunk => chunk.LegalDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.SourceFileName)
            .IsUnique();
    }
}