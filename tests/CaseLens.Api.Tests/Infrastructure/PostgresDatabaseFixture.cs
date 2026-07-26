using CaseLens.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CaseLens.Api.Tests.Infrastructure;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public PostgresDatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresDatabaseFixture>()
            .Build();

        _connectionString =
            configuration.GetConnectionString(
                "CaseLensTestDatabase")
            ?? throw new InvalidOperationException(
                "The test connection string " +
                "'ConnectionStrings:CaseLensTestDatabase' " +
                "was not found in the CaseLens.Api.Tests user secrets.");
    }

    public CaseLensDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CaseLensDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new CaseLensDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                document_chunks,
                legal_documents
            RESTART IDENTITY CASCADE;
            """);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}