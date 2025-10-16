using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using WeatherTelegramBot.Services;

namespace WeatherTelegramBot.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController : ControllerBase
{
    private readonly ITelegramBotClient _botClient;
    private readonly IWeatherService _weatherService;

    public BotController(ITelegramBotClient botClient, IWeatherService weatherService)
    {
        _botClient = botClient;
        _weatherService = weatherService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update.Message?.Text != null)
        {
            await HandleMessage(update.Message);
        }
        return Ok();
    }

    private async Task HandleMessage(Message message)
    {
        var chatId = message.Chat.Id;
        var text = message.Text ?? string.Empty;

        if (text == "/start")
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "🌤️ Добро пожаловать в погодный бот!\n\n" +
                "Отправьте название города, чтобы узнать погоду.\n" +
                "Например: Москва или London");
        }
        else
        {
            var weather = await _weatherService.GetWeatherAsync(text);
            
            if (weather != null)
            {
                var response = $"🌤️ Погода в {weather.Name}:\n" +
                              $"Температура: {weather.Main?.Temp}°C\n" +
                              $"Влажность: {weather.Main?.Humidity}%\n" +
                              $"Состояние: {weather.Weather?[0].Description}";
                
                await _botClient.SendTextMessageAsync(chatId, response);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Город не найден");
            }
        }
    }
}