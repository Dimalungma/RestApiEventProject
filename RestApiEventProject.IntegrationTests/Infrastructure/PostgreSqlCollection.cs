namespace RestApiEventProject.IntegrationTests.Infrastructure;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
{
    public const string CollectionName = "PostgreSql collection";
}