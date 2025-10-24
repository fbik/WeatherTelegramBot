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
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

botClient.StartReceiving(
    updateHandler: updateHandler.HandleUpdateAsync,
    pollingErrorHandler: updateHandler.HandlePollingErrorAsync,
    receiverOptions: receiverOptions
);

Console.WriteLine("🤖 Bot started with Polling, Buttons and 5-Day Forecast!");
app.Run("http://0.0.0.0:8080");

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
        // Обработка callback от кнопок
        if (update.CallbackQuery != null)
        {
            await HandleCallbackQuery(update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();

          //  Console.WriteLine($"📨 Received: {text}");

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
                            text: "🌤️ Укажите город после команды:\n/weather Москва",
                            cancellationToken: cancellationToken);
                        break;

                    case "/forecast":
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "📊 Укажите город после команды:\n/forecast Москва",
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
                      "Доступные команды:\n\n" +
                      "🏙️ /cities - Популярные города\n" +
                      "🌡️ /weather <город> - Текущая погода\n" +
                      "📊 /forecast <город> - Прогноз на 5 дней\n" +
                      "📍 Или просто отправьте название города";

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

        Console.WriteLine($"🔄 Callback received: {data}");

        if (data.StartsWith("city_"))
        {
            var city = data.Substring(5); // Убираем "city_"
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            await HandleWeatherRequest(chatId, city, cancellationToken);
        }
        else if (data.StartsWith("forecast_"))
        {
            var city = data.Substring(9); // Убираем "forecast_"
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            await HandleForecastRequest(chatId, city, cancellationToken);
        }
        else if (data == "show_cities")
        {
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            await ShowCitiesMenu(chatId, cancellationToken);
        }
    }

    private async Task HandleWeatherRequest(long chatId, string city, CancellationToken cancellationToken)
    {
        try
        {
            //Console.WriteLine($"🌤️ Starting weather request for: {city}");

            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);

            var weather = await _weatherService.GetWeatherAsync(city);

           // Console.WriteLine($"📊 Weather service returned: {weather != null}");

            if (weather != null && weather.Main != null)
            {
               // Console.WriteLine($"✅ Weather data: {weather.Name}, Temp: {weather.Main.Temp}");

                var response = $"🌤️ Погода в {weather.Name}:\n" +
                              $"🌡️ Температура: {weather.Main.Temp}°C\n" +
                              $"💧 Влажность: {weather.Main.Humidity}%\n" +
                              $"💨 Ветер: {weather.Wind?.Speed:F1} м/с\n" +
                              $"☁️ {weather.Weather?[0].Description}";

                // Добавляем кнопки для выбора другого города и прогноза
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🏙️ Другой город", "show_cities"),
                        InlineKeyboardButton.WithCallbackData("📊 Прогноз на 5 дней", $"forecast_{city}")
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
                Console.WriteLine($"❌ Weather data is null");
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Не удалось получить погоду для '{city}'. Попробуйте другой город.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HandleWeatherRequest error: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка получения данных о погоде. Попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleForecastRequest(long chatId, string city, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"📊 Starting 5-day forecast request for: {city}");

            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);

            // Используем реальный API для прогноза на 5 дней
            var forecast = await _weatherService.GetWeatherForecastAsync(city);

            if (forecast != null)
            {
                var response = $"📊 Прогноз погоды в {forecast.City} на 5 дней:\n\n" +
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

                // Добавляем кнопку для возврата к городям
                var keyboard = new InlineKeyboardMarkup(new[] {
                    new[] {
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
                    text: $"❌ Не удалось получить прогноз для {city}",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Forecast error: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка получения прогноза погоды",
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"❌ Telegram Polling Error: {exception.Message}");
        await Task.CompletedTask;
    }
}


