using UsersService.Application;
using UsersService.Domain;

namespace RestApiProject.Tests.Infrastructure;

internal sealed class TestJwtTokenGenerator : IJwtTokenGenerator
{
    public string Token { get; set; } = "TEST_JWT_TOKEN";

    public int GenerateCalls { get; private set; }

    public long? LastUserId { get; private set; }

    public string? LastLogin { get; private set; }

    public UserRole? LastRole { get; private set; }

    public string GenerateToken(
        long userId,
        string login,
        UserRole role)
    {
        GenerateCalls++;

        LastUserId = userId;
        LastLogin = login;
        LastRole = role;

        return Token;
    }
}