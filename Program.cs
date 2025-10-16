using Telegram.Bot;  // ← ДОБАВЬТЕ ЭТУ СТРОКУ В НАЧАЛЕ
using WeatherTelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IWeatherService, WeatherService>();

builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var token = builder.Configuration["TelegramBotSettings:BotToken"];
    return new TelegramBotClient(token);
});

// Существующие сервисы Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настройка конвейера HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Простой эндпоинт для проверки работы
app.MapGet("/", () => "Weather Telegram Bot is running! Use Telegram to interact with the bot.");

app.Run();