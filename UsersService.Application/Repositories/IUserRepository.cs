using UsersService.Domain;

namespace UsersService.Application;

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}