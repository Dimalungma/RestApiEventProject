using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Application;
using RestApiEventProject.Domain;

namespace RestApiEventProject.Infrastructure.DataAccess;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .SingleOrDefaultAsync(
                user => user.Login == login,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}