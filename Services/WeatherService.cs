using System.Text.Json;
using WeatherTelegramBot.Models;

namespace WeatherTelegramBot.Servises;

class WeatherService : IWeatherService 
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WeatherService(HttpClient httpClient, IConfiguration configuration) 
    {
      _httpClient = httpClient;
      _apiKey = configuration["e2b2f822ce3942d2a45114617251510"];
      _baseUrl = configuration["https://api.openweathermap.org/data/2.5/"];
    }  

    public async Task<WeatherResponse?> GetWeatherAsync(string sity) {
        try
        {
            var url = $"{_baseUrl}weather?q={city}&appid={_apiKey}&units=metric&lang=ru";
            var response = await _httpClient.GetAsyng(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsyng();
                var watherData = JsonSerializer.Deserialize<WeatherResponse>();
                return watherData; 
            }
            return null;
        }
        catch (System.Exception)
        {
            Console.WriteLine($"Ошибка получения погоды: {ex.Message}");
            return null;
        }
    }


}
    
