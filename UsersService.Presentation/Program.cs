using Microsoft.OpenApi;
using UsersService.Application;
using UsersService.Infrastructure;
using UsersService.Presentation.Extensions;
using UsersService.Presentation.Middleware;
using UsersService.Presentation.Options;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogger();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    const string schemeName = "Bearer";

    options.AddSecurityDefinition(schemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Введите JWT-токен без префикса Bearer"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeName, document)] = []
    });
});

builder.Services.Configure<InitialAdminOptions>(
    builder.Configuration.GetSection(InitialAdminOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.ApplyDatabaseMigrations();

await app.Services.EnsureInitialAdminAsync();

app.UseMiddleware<CustomExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();