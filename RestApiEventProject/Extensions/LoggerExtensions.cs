using Serilog;
namespace RestApiEventProject;

public static class LoggerExtensions
{
    public static void ConfigureLogger(this WebApplicationBuilder builder)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            );

        if (builder.Environment.IsDevelopment())
        {
            loggerConfig = loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Console();
        }

        Log.Logger = loggerConfig.CreateLogger();

        builder.Host.UseSerilog();
    }
}
