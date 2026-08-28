namespace RestApiEventProject.IntegrationTests.Infrastructure;

[Collection(PostgreSqlCollection.CollectionName)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlTestFixture Fixture;

    protected IntegrationTestBase(
        PostgreSqlTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected abstract Task ResetDatabaseAsync();

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}