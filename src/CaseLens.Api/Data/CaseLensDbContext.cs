using CaseLens.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseLens.Api.Data;

public sealed class CaseLensDbContext : DbContext
{
    public CaseLensDbContext(
        DbContextOptions<CaseLensDbContext> options)
        : base(options)
    {
    }

    public DbSet<LegalDocument> LegalDocuments =>
        Set<LegalDocument>();

    public DbSet<DocumentChunk> DocumentChunks =>
        Set<DocumentChunk>();

   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(CaseLensDbContext).Assembly);
}
}