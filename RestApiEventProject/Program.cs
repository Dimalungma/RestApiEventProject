using Microsoft.EntityFrameworkCore;
using RestApiEventProject;
using RestApiEventProject.DataAccess;
using RestApiEventProject.DataAccess.Repositories;
using RestApiEventProject.Middleware;
using RestApiEventProject.Services;
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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Репозитории
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

//Сервисы
builder.Services.AddScoped<IEventService, EventService>(); 
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddHostedService<BookingBackgroundService>();

//Мапперы
builder.Services.AddSingleton<IEventMapper, EventMapper>();
builder.Services.AddSingleton<IBookingMapper, BookingMapper>();


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
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
