using RestApiEventProject;
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

builder.Services.AddSingleton<IEventMapper, EventMapper>();
builder.Services.AddSingleton<IEventService, EventService>(); //Сейчас храним список в сервисе, поэтому Singleton
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddSingleton<IBookingMapper, BookingMapper>();
//TODO: Разделить сервис и хранение


var app = builder.Build();
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
