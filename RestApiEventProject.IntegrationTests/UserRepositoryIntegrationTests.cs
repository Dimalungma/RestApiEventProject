using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Domain;
using RestApiEventProject.Infrastructure.DataAccess;
using RestApiEventProject.IntegrationTests.Infrastructure;

namespace RestApiEventProject.IntegrationTests;

public class UserRepositoryIntegrationTests : IntegrationTestBase
{
    public UserRepositoryIntegrationTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_Should_Save_User_To_PostgreSql()
    {
        long userId;

        await using (var context = Fixture.CreateDbContext())
        {
            // Arrange
            var repository = new UserRepository(context);
            var user = User.Create(
                "integration-user",
                "TEST_PASSWORD_HASH",
                UserRole.User);

            // Act
            await repository.AddAsync(user);
            await repository.SaveChangesAsync();

            userId = user.Id;
        }

        await using var assertContext = Fixture.CreateDbContext();

        // Assert
        var savedUser = await assertContext.Users.FindAsync(userId);

        Assert.True(userId > 0);
        Assert.NotNull(savedUser);
        Assert.Equal("integration-user", savedUser.Login);
        Assert.Equal("TEST_PASSWORD_HASH", savedUser.PasswordHash);
        Assert.Equal(UserRole.User, savedUser.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_Should_Return_User_When_Login_Exists()
    {
        long userId;

        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var repository = new UserRepository(seedContext);
            var user = User.Create(
                "existing-user",
                "TEST_PASSWORD_HASH",
                UserRole.Admin);

            await repository.AddAsync(user);
            await repository.SaveChangesAsync();

            userId = user.Id;
        }

        await using var queryContext = Fixture.CreateDbContext();
        var queryRepository = new UserRepository(queryContext);

        // Act
        var result = await queryRepository.GetByLoginAsync("existing-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("existing-user", result.Login);
        Assert.Equal("TEST_PASSWORD_HASH", result.PasswordHash);
        Assert.Equal(UserRole.Admin, result.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_Should_Return_Null_When_Login_Does_Not_Exist()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByLoginAsync("missing-user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_Should_Not_Allow_Duplicate_Login()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();
        var repository = new UserRepository(context);

        var firstUser = User.Create(
            "duplicate-user",
            "FIRST_PASSWORD_HASH",
            UserRole.User);

        var secondUser = User.Create(
            "duplicate-user",
            "SECOND_PASSWORD_HASH",
            UserRole.Admin);

        await repository.AddAsync(firstUser);
        await repository.SaveChangesAsync();

        await repository.AddAsync(secondUser);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () =>
            {
                await repository.SaveChangesAsync();
            });

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task AddAsync_Should_Generate_Unique_Ids_For_Different_Users()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();
        var repository = new UserRepository(context);

        var firstUser = User.Create(
            "first-user",
            "FIRST_PASSWORD_HASH",
            UserRole.User);

        var secondUser = User.Create(
            "second-user",
            "SECOND_PASSWORD_HASH",
            UserRole.User);

        // Act
        await repository.AddAsync(firstUser);
        await repository.AddAsync(secondUser);
        await repository.SaveChangesAsync();

        // Assert
        Assert.True(firstUser.Id > 0);
        Assert.True(secondUser.Id > 0);
        Assert.NotEqual(firstUser.Id, secondUser.Id);
    }
}