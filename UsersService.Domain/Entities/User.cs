namespace UsersService.Domain;

public class User
{
    private User() //Для EF
    {
        Login = null!;
        PasswordHash = null!;
    }

    public long Id { get; set; }

    public required string Login { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public static User Create(
        string login,
        string passwordHash,
        UserRole role)
    {
        return new User
        {
            Login = login,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
