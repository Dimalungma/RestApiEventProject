namespace RestApiEventProject.Domain;

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

    public List<Booking> Bookings { get; set; } = []; //Чтобы сразу можно было привязать обратно 1:М бронирования к пользователю

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
