using RestApiEventProject.Domain;

namespace RestApiEventProject.Application;

public interface IUserService
{
    Task<UserRegisterError?> RegisterAsync(
        string login,
        string password,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(string? Token, UserLoginError? Error)> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default);
}
