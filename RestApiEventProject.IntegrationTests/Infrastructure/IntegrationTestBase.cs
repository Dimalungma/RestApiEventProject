namespace RestApiEventProject.IntegrationTests.Infrastructure;

[Collection(PostgreSqlCollection.CollectionName)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlTestFixture Fixture;

    protected IntegrationTestBase(PostgreSqlTestFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}