using WeatherTelegramBot.Models;

namespace WeatherTelegramBot.Servises;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string sity)
}
    
