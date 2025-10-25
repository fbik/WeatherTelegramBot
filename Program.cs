using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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

var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
var updateHandler = app.Services.GetRequiredService<UpdateHandler>();
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

botClient.StartReceiving(
    updateHandler: updateHandler.HandleUpdateAsync,
    pollingErrorHandler: updateHandler.HandlePollingErrorAsync,
    receiverOptions: receiverOptions
);
Console.WriteLine("✅ Bot started with Polling, Buttons and 5-Day Forecast!");
app.Run();

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IWeatherService _weatherService;
    private static bool _unauthorizedLogged = false; // Статическая переменная для однократного логирования

    public UpdateHandler(ITelegramBotClient botClient, IWeatherService weatherService)
    {
        _botClient = botClient;
        _weatherService = weatherService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            await HandleCallbackQuery(update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();

            try
            {
                switch (text.ToLower())
                {
                    case "/start":
                        await ShowMainMenu(chatId, cancellationToken);
                        break;

                    case "/weather":
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "📍 Укажите город после команды:\n/weather Москва",
                            cancellationToken: cancellationToken);
                        break;

                    case "/forecast":
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "📅 Укажите город после команды:\n/forecast Москва",
                            cancellationToken: cancellationToken);
                        break;

                    case var cmd when text.StartsWith("/weather "):
                        var cityName = text.Substring(9).Trim();
                        await HandleWeatherRequest(chatId, cityName, cancellationToken);
                        break;

                    case var cmd when text.StartsWith("/forecast "):
                        var forecastCity = text.Substring(10).Trim();
                        await HandleForecastRequest(chatId, forecastCity, cancellationToken);
                        break;

                    case "/cities":
                        await ShowCitiesMenu(chatId, cancellationToken);
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

    private async Task ShowMainMenu(long chatId, CancellationToken cancellationToken)
    {
        var menuText = "🌤️ Добро пожаловать в погодный бот!\n\n" +
                      "Доступные команды:\n" +
                      "🏙️ /cities - Популярные города\n" +
                      "🌡️ /weather <город> - Текущая погода\n" +
                      "📅 /forecast <город> - Прогноз на 5 дней\n" +
                      "💬 Или просто отправьте название города";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: menuText,
            cancellationToken: cancellationToken);
    }

    private async Task ShowCitiesMenu(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏙️ Москва", "city_Moscow"),
                InlineKeyboardButton.WithCallbackData("🏙️ СПб", "city_St Petersburg")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏙️ Нью-Йорк", "city_New York"),
                InlineKeyboardButton.WithCallbackData("🏙️ Лондон", "city_London")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏙️ Париж", "city_Paris"),
                InlineKeyboardButton.WithCallbackData("🏙️ Стокгольм", "city_Stockholm")
            },
            new[]
            {
            InlineKeyboardButton.WithCallbackData("🏙️ Бишкек", "city_Bishkek"),
            InlineKeyboardButton.WithCallbackData("🏙️ София", "city_Sofia")
        },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏙️ Дубай", "city_Dubai"),
                InlineKeyboardButton.WithCallbackData("🏙️ Воронеж", "city_Voronezh")
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "🏙️ Выберите город:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data;

        // СРАЗУ отвечаем на callback чтобы Telegram знал что запрос принят
        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (data.StartsWith("city_"))
        {
            var city = data.Substring(5);
            
            // Запускаем обработку в фоне без ожидания
            _ = Task.Run(async () => 
            {
                try
                {
                    await HandleWeatherRequest(chatId, city, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Background task error: {ex.Message}");
                }
            });
        }
        else if (data == "show_cities")
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    await ShowCitiesMenu(chatId, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Background task error: {ex.Message}");
                }
            });
        }
        else if (data.StartsWith("forecast_"))
        {
            var city = data.Substring(9);
            _ = Task.Run(async () => 
            {
                try
                {
                    await HandleForecastRequest(chatId, city, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Background task error: {ex.Message}");
                }
            });
        }
    }

    private async Task HandleWeatherRequest(long chatId, string city, CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            var weather = await _weatherService.GetWeatherAsync(city);

            if (weather != null && weather.Main != null)
            {
                Console.WriteLine($"✅ Weather for {city}: {weather.Main.Temp}°C");

                var response = $"🌤️ Погода в {weather.Name}:\n" +
                              $"🌡️ Температура: {weather.Main.Temp}°C\n" +
                              $"💧 Влажность: {weather.Main.Humidity}%\n" +
                              $"💨 Ветер: {weather.Wind?.Speed:F1} м/с\n" +
                              $"☁️ {weather.Weather?[0].Description}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🏙️ Другой город", "show_cities"),
                        InlineKeyboardButton.WithCallbackData("📅 Прогноз на 5 дней", $"forecast_{city}")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    $"❌ Не удалось получить погоду для '{city}'. Попробуйте другой город.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HandleWeatherRequest error: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                "❌ Ошибка получения данных о погоде. Попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleForecastRequest(long chatId, string city, CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            var forecast = await _weatherService.GetWeatherForecastAsync(city);

            if (forecast != null)
            {
                var response = $"📅 Прогноз погоды в {forecast.City} на 5 дней:\n\n" +
                              $"📅 {forecast.Day1} (завтра)\n" +
                              $"🌡️ {forecast.Temp1}°C, {forecast.Condition1}\n\n" +
                              $"📅 {forecast.Day2}\n" +
                              $"🌡️ {forecast.Temp2}°C, {forecast.Condition2}\n\n" +
                              $"📅 {forecast.Day3}\n" +
                              $"🌡️ {forecast.Temp3}°C, {forecast.Condition3}\n\n" +
                              $"📅 {forecast.Day4}\n" +
                              $"🌡️ {forecast.Temp4}°C, {forecast.Condition4}\n\n" +
                              $"📅 {forecast.Day5}\n" +
                              $"🌡️ {forecast.Temp5}°C, {forecast.Condition5}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🏙️ Выбрать город", "show_cities")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    $"❌ Не удалось получить прогноз для {city}",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Forecast error: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                "❌ Ошибка получения прогноза погоды",
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Логируем Unauthorized только один раз
        if (exception.Message.Contains("Unauthorized"))
        {
            if (!_unauthorizedLogged)
            {
                Console.WriteLine($"❌ BOT TOKEN ERROR: Telegram Bot Token is invalid or revoked.");
                Console.WriteLine($"❌ Please create a new bot in @BotFather and update the TelegramBotSettings__BotToken environment variable in Render.com");
                _unauthorizedLogged = true;
            }
            await Task.CompletedTask;
            return;
        }
        
        // Для других ошибок - нормальное логирование
        Console.WriteLine($"❌ Telegram Polling Error: {exception.Message}");
        await Task.CompletedTask;
    }
}

// Модель для прогноза погоды
public class WeatherForecast
{
    public string? City { get; set; }
    public string? Day1 { get; set; }
    public double Temp1 { get; set; }
    public string? Condition1 { get; set; }

    public string? Day2 { get; set; }
    public double Temp2 { get; set; }
    public string? Condition2 { get; set; }

    public string? Day3 { get; set; }
    public double Temp3 { get; set; }
    public string? Condition3 { get; set; }

    public string? Day4 { get; set; }
    public double Temp4 { get; set; }
    public string? Condition4 { get; set; }

    public string? Day5 { get; set; }
    public double Temp5 { get; set; }
    public string? Condition5 { get; set; }
}


