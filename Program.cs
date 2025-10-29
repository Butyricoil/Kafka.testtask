using MSDisTestTask.Data;
using MSDisTestTask.Services;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== Запуск приложения MSDisTestTask ===");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<EventObservable>();

var usePostgreSQL = true;
Console.WriteLine($"[Program] Используемое хранилище: {(usePostgreSQL ? "PostgreSQL" : "File")}");
Console.WriteLine($"[Program] KAFKA_BOOTSTRAP_SERVERS: {builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"]}");
Console.WriteLine($"[Program] KAFKA_TOPIC: {builder.Configuration["KAFKA_TOPIC"]}");
Console.WriteLine($"[Program] POSTGRES_CONNECTION_STRING: {builder.Configuration["POSTGRES_CONNECTION_STRING"]}");

if (usePostgreSQL)
{
    builder.Services.AddScoped<IDataStorage, PostgreSqlDataStorage>();
}
else
{
    builder.Services.AddScoped<IDataStorage, FileDataStorage>();
}

builder.Services.AddScoped<EventObserver>();
builder.Services.AddHostedService<KafkaConsumer>();

var app = builder.Build();

Console.WriteLine("[Program] Конфигурация сервисов завершена");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("[Program] Swagger включен для Development окружения");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var eventObservable = scope.ServiceProvider.GetRequiredService<EventObservable>();
    var dataStorage = scope.ServiceProvider.GetRequiredService<IDataStorage>();
    var eventObserver = new EventObserver(dataStorage);
    
    eventObservable.Subscribe(eventObserver);
    Console.WriteLine("[Program] EventObserver подписан на EventObservable");
}

Console.WriteLine("[Program] Приложение готово к работе");
Console.WriteLine("=== Доступные эндпоинты ===");
Console.WriteLine("GET /api/stats - получить всю статистику");
Console.WriteLine("GET /api/stats/user/{userId} - получить статистику пользователя");
Console.WriteLine("===============================");

app.Run();