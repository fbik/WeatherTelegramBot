 using WeatherTelegramBot.Models;

namespace WeatherTelegramBot.Services;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city);

}

