using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestApiProject.Tests.Infrastructure;
using UsersService.Application;
using UsersService.Domain;

namespace RestApiProject.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterAsync_Should_Save_User_With_Hashed_Password_And_User_Role()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var passwordHasher =
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Act
        var error = await userService.RegisterAsync(
            "test-user",
            "safePassword",
            isAdmin: false);

        // Assert
        error.Should().BeNull();

        var savedUser =
            await userRepository.GetByLoginAsync("test-user");

        savedUser.Should().NotBeNull();
        savedUser!.Login.Should().Be("test-user");
        savedUser.Role.Should().Be(UserRole.User);
        savedUser.PasswordHash.Should().NotBe("safePassword");

        passwordHasher
            .Verify("safePassword", savedUser.PasswordHash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_Should_Save_User_With_Admin_Role_When_IsAdmin_Is_True()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Act
        var error = await userService.RegisterAsync(
            "admin-user",
            "safePassword",
            isAdmin: true);

        // Assert
        error.Should().BeNull();

        var savedUser =
            await userRepository.GetByLoginAsync("admin-user");

        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Admin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_Should_Return_InvalidLogin_When_Login_Is_Empty(
        string login)
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Act
        var error = await userService.RegisterAsync(
            login,
            "safePassword",
            isAdmin: false);

        // Assert
        error.Should().Be(UserRegisterError.InvalidLogin);

        var savedUser =
            await userRepository.GetByLoginAsync(login);

        savedUser.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_Should_Return_InvalidPassword_When_Password_Is_Empty(
        string password)
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Act
        var error = await userService.RegisterAsync(
            "test-user",
            password,
            isAdmin: false);

        // Assert
        error.Should().Be(UserRegisterError.InvalidPassword);

        var savedUser =
            await userRepository.GetByLoginAsync("test-user");

        savedUser.Should().BeNull();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1234password")]
    [InlineData("aaaa")]
    public async Task RegisterAsync_Should_Return_PasswordTooSimple_When_Password_Is_Too_Simple(
        string password)
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var userRepository =
            scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Act
        var error = await userService.RegisterAsync(
            "test-user",
            password,
            isAdmin: false);

        // Assert
        error.Should().Be(UserRegisterError.PasswordTooSimple);

        var savedUser =
            await userRepository.GetByLoginAsync("test-user");

        savedUser.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_Should_Return_LoginAlreadyExists_When_Login_Is_Already_Registered()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        using (var firstScope = provider.CreateScope())
        {
            var userService =
                firstScope.ServiceProvider
                    .GetRequiredService<IUserService>();

            var firstError =
                await userService.RegisterAsync(
                    "existing-user",
                    "safePassword",
                    isAdmin: false);

            firstError.Should().BeNull();
        }

        using var secondScope = provider.CreateScope();

        var secondUserService =
            secondScope.ServiceProvider
                .GetRequiredService<IUserService>();

        // Act
        var error =
            await secondUserService.RegisterAsync(
                "existing-user",
                "anotherPassword",
                isAdmin: false);

        // Assert
        error.Should().Be(UserRegisterError.LoginAlreadyExists);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_InvalidCredentials_When_User_Does_Not_Exist()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        var tokenGenerator =
            scope.ServiceProvider
                .GetRequiredService<TestJwtTokenGenerator>();

        // Act
        var (token, error) =
            await userService.LoginAsync(
                "missing-user",
                "safePassword");

        // Assert
        token.Should().BeNull();
        error.Should().Be(UserLoginError.InvalidCredentials);
        tokenGenerator.GenerateCalls.Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_InvalidCredentials_When_Password_Is_Incorrect()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        using (var registerScope = provider.CreateScope())
        {
            var userService =
                registerScope.ServiceProvider
                    .GetRequiredService<IUserService>();

            var registerError =
                await userService.RegisterAsync(
                    "test-user",
                    "correctPassword",
                    isAdmin: false);

            registerError.Should().BeNull();
        }

        using var loginScope = provider.CreateScope();

        var loginService =
            loginScope.ServiceProvider
                .GetRequiredService<IUserService>();

        var tokenGenerator =
            loginScope.ServiceProvider
                .GetRequiredService<TestJwtTokenGenerator>();

        // Act
        var (token, error) =
            await loginService.LoginAsync(
                "test-user",
                "wrongPassword");

        // Assert
        token.Should().BeNull();
        error.Should().Be(UserLoginError.InvalidCredentials);
        tokenGenerator.GenerateCalls.Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Token_When_Credentials_Are_Valid()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        long userId;

        using (var registerScope = provider.CreateScope())
        {
            var userService =
                registerScope.ServiceProvider
                    .GetRequiredService<IUserService>();

            var userRepository =
                registerScope.ServiceProvider
                    .GetRequiredService<IUserRepository>();

            var registerError =
                await userService.RegisterAsync(
                    "test-admin",
                    "safePassword",
                    isAdmin: true);

            registerError.Should().BeNull();

            var user =
                await userRepository.GetByLoginAsync("test-admin");

            user.Should().NotBeNull();

            userId = user!.Id;
        }

        using var loginScope = provider.CreateScope();

        var loginService =
            loginScope.ServiceProvider
                .GetRequiredService<IUserService>();

        var tokenGenerator =
            loginScope.ServiceProvider
                .GetRequiredService<TestJwtTokenGenerator>();

        // Act
        var (token, error) =
            await loginService.LoginAsync(
                "test-admin",
                "safePassword");

        // Assert
        error.Should().BeNull();
        token.Should().Be("TEST_JWT_TOKEN");

        tokenGenerator.GenerateCalls.Should().Be(1);
        tokenGenerator.LastUserId.Should().Be(userId);
        tokenGenerator.LastLogin.Should().Be("test-admin");
        tokenGenerator.LastRole.Should().Be(UserRole.Admin);
    }
}