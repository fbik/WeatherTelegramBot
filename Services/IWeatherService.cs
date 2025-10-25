using WeatherTelegramBot.Models;


namespace WeatherTelegramBot.Services;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city);
    Task<WeatherForecast?> GetWeatherForecastAsync(string city);
}