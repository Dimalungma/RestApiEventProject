using Microsoft.Extensions.Options;
using UsersService.Application;
using UsersService.Presentation.Options;

namespace UsersService.Presentation.Extensions;

public static class InitialAdminExtensions
{
    public static async Task EnsureInitialAdminAsync(
        this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<InitialAdminOptions>>()
            .Value;

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("InitialAdmin");

        if (string.IsNullOrWhiteSpace(options.Login) ||
            string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning(
                "Начальный администратор не создан: InitialAdmin не настроен");

            return;
        }

        var userService =
            scope.ServiceProvider.GetRequiredService<IUserService>();

        if (await userService.IsAdminAsync(options.Login))
        {
            logger.LogInformation(
                $"Начальный администратор {options.Login} уже существует");

            return;
        }

        var error = await userService.RegisterAsync(
            options.Login,
            options.Password,
            true);

        if (error == UserRegisterError.LoginAlreadyExists)
        {
            logger.LogError(
                $"Начальный администратор не создан: логин {options.Login} уже занят пользователем без роли Admin");

            return;
        }

        if (error is not null)
        {
            logger.LogError(
                $"Начальный администратор не создан. Ошибка: {error}");

            return;
        }

        logger.LogInformation(
            $"Создан начальный администратор {options.Login}");
    }
}