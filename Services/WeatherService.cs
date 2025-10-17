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
                Console.WriteLine($"📄 JSON received, length: {json.Length}");
                
                // ДЛЯ ДЕБАГА: выведем весь JSON
                Console.WriteLine($"📋 FULL JSON: {json}");
                
                var weatherData = JsonSerializer.Deserialize<WeatherApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (weatherData != null && weatherData.Location != null && weatherData.Current != null)
                {
                    Console.WriteLine($"✅ Successfully parsed weather data");
                    Console.WriteLine($"📍 City: {weatherData.Location.Name}");
                    Console.WriteLine($"🌡️ TempC: {weatherData.Current.TempC}");
                    Console.WriteLine($"💧 Humidity: {weatherData.Current.Humidity}");
                    Console.WriteLine($"💨 WindKph: {weatherData.Current.WindKph}");
                    
                    return new WeatherResponse
                    {
                        Name = weatherData.Location.Name ?? city,
                        Main = new MainData
                        {
                            Temp = weatherData.Current.TempC,
                            Humidity = weatherData.Current.Humidity,
                            Feels_Like = weatherData.Current.FeelslikeC
                        },
                        Weather = new[]
                        {
                            new Weather
                            {
                                Main = weatherData.Current.Condition?.Text,
                                Description = weatherData.Current.Condition?.Text
                            }
                        },
                        Wind = new Wind
                        {
                            Speed = weatherData.Current.WindKph / 3.6 // км/ч → м/с
                        }
                    };
                }
                else
                {
                    Console.WriteLine("❌ Failed to deserialize JSON or missing data");
                }
            }
            else
            {
                Console.WriteLine($"❌ API error: {response.StatusCode}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Weather service error: {ex.Message}");
            return null;
        }
    }
}

// Модели для WeatherAPI - ДОЛЖНЫ ТОЧНО СОВПАДАТЬ С JSON
public class WeatherApiResponse
{
    public Location? Location { get; set; }
    public Current? Current { get; set; }
}

public class Location
{
    public string? Name { get; set; }
}

public class Current
{
    public double Temp_C { get; set; }  // ← ВНИМАНИЕ: Temp_C а не TempC!
    public int Humidity { get; set; }
    public double Feelslike_C { get; set; }  // ← Feelslike_C а не FeelslikeC!
    public double Wind_Kph { get; set; }  // ← Wind_Kph а не WindKph!
    public Condition? Condition { get; set; }
}

public class Condition
{
    public string? Text { get; set; }
}