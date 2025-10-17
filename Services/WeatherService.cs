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
            var url = $"{_baseUrl}current.json?key={_apiKey}&q={city}";
            Console.WriteLine($"🔍 Requesting: {url}");
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📄 JSON received");
                
                // Парсим реальный JSON
                var weatherData = JsonSerializer.Deserialize<WeatherApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (weatherData?.location != null && weatherData.current != null)
                {
                    Console.WriteLine($"✅ Real data - City: {weatherData.location.name}, Temp: {weatherData.current.temp_c}°C");
                    
                    return new WeatherResponse
                    {
                        Name = weatherData.location.name ?? city,
                        Main = new MainData
                        {
                            Temp = weatherData.current.temp_c,
                            Humidity = weatherData.current.humidity,
                            Feels_Like = weatherData.current.feelslike_c
                        },
                        Weather = new[]
                        {
                            new Weather
                            {
                                Description = weatherData.current.condition?.text
                            }
                        },
                        Wind = new Wind
                        {
                            Speed = weatherData.current.wind_kph / 3.6 // км/ч → м/с
                        }
                    };
                }
            }
            
            Console.WriteLine($"❌ API error: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Weather service error: {ex.Message}");
            return null;
        }
    }
}

// Модели ТОЧНО как в JSON (нижний регистр!)
public class WeatherApiResponse
{
    public Location? location { get; set; }
    public Current? current { get; set; }
}

public class Location
{
    public string? name { get; set; }
}

public class Current
{
    public double temp_c { get; set; }
    public int humidity { get; set; }
    public double feelslike_c { get; set; }
    public double wind_kph { get; set; }
    public Condition? condition { get; set; }
}

public class Condition
{
    public string? text { get; set; }
}