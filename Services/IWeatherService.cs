using WeatherTelegramBot.Models;
using WeatherTelegramBot.Services;

namespace WeatherTelegramBot.Services;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city);
    Task<WeatherForecast?> GetWeatherForecastAsync(string city); // Добавлен знак ?
}