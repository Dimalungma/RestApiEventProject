using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Domain;

namespace RestApiEventProject.Infrastructure.DataAccess;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); //Автопоиск всех конфигураций в сборке. Cмотри DataAccess/Configurations

        base.OnModelCreating(modelBuilder); //На случай расширения базовой логики
    }
}
