using UsersService.Application;
using UsersService.Infrastructure;
using UsersService.Presentation.Extensions;
using UsersService.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogger();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.ApplyDatabaseMigrations();

app.UseMiddleware<CustomExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();