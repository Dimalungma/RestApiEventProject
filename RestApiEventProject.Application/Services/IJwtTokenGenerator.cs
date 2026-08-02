using RestApiEventProject.Domain;

namespace RestApiEventProject.Application;

public interface IJwtTokenGenerator
{
    string GenerateToken(long userId, string login, UserRole role);
}
