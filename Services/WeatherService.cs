using System.Text.Json;
using WeatherTelegramBot.Models;

namespace WeatherTelegramBot.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _baseUrl;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApiSettings:ApiKey"];
        _baseUrl = configuration["WeatherApiSettings:BaseUrl"];
    }

    public async Task<WeatherResponse?> GetWeatherAsync(string city)
    {
        try
        {
            var url = $"{_baseUrl}weather?q={city}&appid={_apiKey}&units=metric&lang=ru";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<WeatherResponse>(json);
                return weatherData;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения погоды: {ex.Message}");
            return null;
        }
    }
}