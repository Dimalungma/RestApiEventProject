using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Application;
using RestApiEventProject.Infrastructure;
using RestApiEventProject.Infrastructure.DataAccess;
using RestApiEventProject.Presentation.Extensions;
using RestApiEventProject.Presentation.Middleware;
using RestApiEventProject.Presentation.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogger(); //Extension метод с конфигурацией Serilog'а
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

app.UseAuthorization();

app.MapControllers();

app.Run();
