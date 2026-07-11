using RestApiEventProject.Application;
using RestApiEventProject.Infrastructure;
using RestApiEventProject.Presentation.Extensions;
using RestApiEventProject.Presentation.Middleware;
using RestApiEventProject.Presentation.Services;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RestApiEventProject.Infrastructure.Security;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogger(); //Extension метод с конфигурацией Serilog'а
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var jwtOptions = builder.Configuration
                     .GetRequiredSection(JwtOptions.SectionName)
                     .Get<JwtOptions>()
                 ?? throw new InvalidOperationException(
                     "Не удалось загрузить настройки JWT.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    // Путь к XML-файлу с документацией
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

//Теперь все DI по слоям в отдельных проектах, смотрите в них
builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<BookingBackgroundService>();

var app = builder.Build();

app.Services.ApplyDatabaseMigrations();

app.UseMiddleware<CustomExceptionHandler>();

// Configure the HTTP request pipeline.
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
