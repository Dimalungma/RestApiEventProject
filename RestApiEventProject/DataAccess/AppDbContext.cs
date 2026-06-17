using RestApiEventProject.Models;
using Microsoft.EntityFrameworkCore;

namespace RestApiEventProject.DataAccess;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); //Автопоиск всех конфигураций в сборке. Cмотри DataAccess/Configurations

        base.OnModelCreating(modelBuilder); //На случай расширения базовой логики
    }
}
