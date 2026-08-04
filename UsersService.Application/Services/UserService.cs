using UsersService.Domain;

namespace UsersService.Application;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    private const int MinPasswordLength = 4;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<UserRegisterError?> RegisterAsync(
        string login,
        string password,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return UserRegisterError.InvalidLogin;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return UserRegisterError.InvalidPassword;
        }

        if (IsPasswordTooSimple(password)) //Решил добавить проверки на самые банальные пароли
        {
            return UserRegisterError.PasswordTooSimple;
        }

        var existingUser = await _userRepository.GetByLoginAsync(
            login,
            cancellationToken);

        if (existingUser is not null)
        {
            return UserRegisterError.LoginAlreadyExists;
        }

        var passwordHash = _passwordHasher.Hash(password);
        var role = isAdmin //Я не уверен, как ещё разделить слои - Presentation не знает UserRole, а Application вряд ли должен знать Claims
            ? UserRole.Admin
            : UserRole.User;

        var user = User.Create(
            login,
            passwordHash,
            role);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return null;
    }

    public async Task<(string? Token, UserLoginError? Error)> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByLoginAsync(
            login,
            cancellationToken);

        if (user is null ||
            !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return (null, UserLoginError.InvalidCredentials);
        }

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Login,
            user.Role);

        return (token, null);
    }

    private static bool IsPasswordTooSimple(string password) //Решил добавить проверки на самые банальные пароли
    {
        if (password.Length < MinPasswordLength) //Слишком короткий пароль
        {
            return true;
        }

        if (password.StartsWith("1234", StringComparison.Ordinal)) //Начинается с 123 и т.п.
        {
            return true;
        }

        if (password.All(character => character == password[0])) //Одинаковые символы (111111....)
        {
            return true;
        }

        return false;
    }
}