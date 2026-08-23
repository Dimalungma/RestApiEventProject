namespace UsersService.Presentation.Options;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}