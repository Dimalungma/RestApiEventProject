using Microsoft.EntityFrameworkCore;
using UsersService.Domain;

namespace UsersService.Infrastructure.DataAccess;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); //Автопоиск всех конфигураций в сборке. Cмотри DataAccess/Configurations

        base.OnModelCreating(modelBuilder); //На случай расширения базовой логики
    }
}
