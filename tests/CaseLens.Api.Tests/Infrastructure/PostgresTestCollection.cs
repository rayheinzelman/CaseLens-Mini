namespace CaseLens.Api.Tests.Infrastructure;

[CollectionDefinition(
    "PostgreSQL",
    DisableParallelization = true)]
public sealed class PostgresTestCollection
    : ICollectionFixture<PostgresDatabaseFixture>
{
}