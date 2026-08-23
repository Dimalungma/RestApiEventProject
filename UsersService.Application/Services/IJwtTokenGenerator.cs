using UsersService.Domain;

namespace UsersService.Application;

public interface IJwtTokenGenerator
{
    string GenerateToken(long userId, string login, UserRole role);
}
