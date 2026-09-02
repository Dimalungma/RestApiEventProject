using Serilog;
using Serilog.Formatting.Compact;

namespace EventsService.Presentation.Extensions;

public static class LoggerExtensions
{
    public static void ConfigureLogger(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    path: "logs/events-service-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7));
    }
}