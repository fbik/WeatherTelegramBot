using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WeatherTelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IWeatherService, WeatherService>();

builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var token = builder.Configuration["TelegramBotSettings:BotToken"];
    
    if (string.IsNullOrEmpty(token) || token == "YOUR_BOT_TOKEN_HERE")
    {
        throw new ArgumentNullException(nameof(token), "Bot token is not configured");
    }
    
    Console.WriteLine($"✅ Telegram Bot Client initialized");
    return new TelegramBotClient(token);
});

builder.Services.AddScoped<UpdateHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Weather Telegram Bot is running!");

// Запускаем polling
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
var updateHandler = app.Services.GetRequiredService<UpdateHandler>();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message }
};

botClient.StartReceiving(
    updateHandler: updateHandler,
    receiverOptions: receiverOptions
);

Console.WriteLine("🤖 Bot started with Polling");
app.Run();

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IWeatherService _weatherService;

    public UpdateHandler(ITelegramBotClient botClient, IWeatherService weatherService)
    {
        _botClient = botClient;
        _weatherService = weatherService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();
            
            Console.WriteLine($"📨 Received: {text}");

            try
            {
                switch (text.ToLower())
                {
                    case "/start":
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "🌤️ Добро пожаловать в погодный бот!\n\n" +
                                  "Доступные команды:\n" +
                                  "/start - показать это сообщение\n" +
                                  "/weather <город> - узнать погоду\n" +
                                  "Или просто отправьте название города",
                            cancellationToken: cancellationToken);
                        break;

                    case "/weather":
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❓ Укажите город после команды:\n/weather Москва",
                            cancellationToken: cancellationToken);
                        break;

                    case var cmd when text.StartsWith("/weather "):
                        var cityName = text.Substring(9).Trim();
                        await HandleWeatherRequest(chatId, cityName, cancellationToken);
                        break;

                    default:
                        if (text.StartsWith("/"))
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "❌ Неизвестная команда. Используйте /start для списка команд",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await HandleWeatherRequest(chatId, text, cancellationToken);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка. Попробуйте позже.",
                    cancellationToken: cancellationToken);
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task HandleWeatherRequest(long chatId, string city, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"🔍 Starting weather request for: {city}");
            
            // Простая версия без лишних параметров
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            
            var weather = await _weatherService.GetWeatherAsync(city);
            
            Console.WriteLine($"📊 Weather service returned: {weather != null}");
            
            if (weather != null && weather.Main != null)
            {
                Console.WriteLine($"✅ Weather data: {weather.Name}, Temp: {weather.Main.Temp}");
                
                var response = $"🌤️ Погода в {weather.Name}:\n" +
                              $"🌡️ Температура: {weather.Main.Temp}°C\n" +
                              $"💧 Влажность: {weather.Main.Humidity}%\n" +
                              $"💨 Ветер: {weather.Wind?.Speed:F1} м/с\n" +
                              $"☁️ {weather.Weather?[0].Description}";

                await _botClient.SendTextMessageAsync(chatId, response, cancellationToken: cancellationToken);
            }
            else
            {
                Console.WriteLine($"❌ Weather data is null");
                await _botClient.SendTextMessageAsync(chatId, $"❌ Не удалось получить погоду для '{city}'. Попробуйте другой город.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HandleWeatherRequest error: {ex.Message}");
            await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка получения данных о погоде. Попробуйте позже.", cancellationToken: cancellationToken);
        }
    }
}
