using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiEventProject.Domain;

namespace RestApiEventProject.Infrastructure.DataAccess;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .UseIdentityByDefaultColumn();

        builder.Property(user => user.Login)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(user => user.Login)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);
    }
}