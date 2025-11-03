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
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<WeatherApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (weatherData?.location != null && weatherData.current != null)
                {
                    Console.WriteLine($"✅ Weather data for {weatherData.location.name}: {weatherData.current.temp_c}°C");

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
                            Speed = weatherData.current.wind_kph / 3.6
                        }
                    };
                }
            }
            else
            {
                Console.WriteLine($"❌ Weather API returned: {response.StatusCode}");
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Weather service error: {ex.Message}");
            return null;
        }
    }

    public async Task<WeatherForecast?> GetWeatherForecastAsync(string city)
    {
        try
        {
            // БЕСПЛАТНЫЙ ТАРИФ: используем days=1 (максимум для бесплатного)
            var url = $"{_baseUrl}forecast.json?key={_apiKey}&q={city}&days=1";
            Console.WriteLine($"📍 Requesting 1-day forecast (free tier): {url}");
            
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var forecastData = JsonSerializer.Deserialize<ForecastApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (forecastData?.forecast?.forecastday != null && forecastData.forecast.forecastday.Length >= 1)
                {
                    var tomorrow = forecastData.forecast.forecastday[0];

                    Console.WriteLine($"✅ Tomorrow's forecast for: {forecastData.location?.name}");

                    // Для бесплатного тарифа показываем только завтрашний день
                    return new WeatherForecast
                    {
                        City = forecastData.location?.name ?? city,
                        Day1 = "Завтра",
                        Temp1 = tomorrow.day.maxtemp_c,
                        Condition1 = tomorrow.day.condition?.text,

                        // Остальные дни не доступны в бесплатном тарифе
                        Day2 = "-",
                        Temp2 = 0,
                        Condition2 = "Недоступно в бесплатном тарифе",

                        Day3 = "-",
                        Temp3 = 0, 
                        Condition3 = "Недоступно в бесплатном тарифе",

                        Day4 = "",
                        Temp4 = 0,
                        Condition4 = "",

                        Day5 = "",
                        Temp5 = 0,
                        Condition5 = ""
                    };
                }
                else
                {
                    Console.WriteLine($"❌ No forecast data received");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"❌ Forecast API error: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Forecast service error: {ex.Message}");
            return null;
        }
    }

    // Модели для текущей погоды
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

    // Модели для прогноза погоды
    public class ForecastApiResponse
    {
        public ForecastLocation? location { get; set; }
        public ForecastData? forecast { get; set; }
    }

    public class ForecastLocation
    {
        public string? name { get; set; }
    }

    public class ForecastData
    {
        public ForecastDay[]? forecastday { get; set; }
    }

    public class ForecastDay
    {
        public string? date { get; set; }
        public DayData? day { get; set; }
    }

    public class DayData
    {
        public double maxtemp_c { get; set; }
        public double mintemp_c { get; set; }
        public Condition? condition { get; set; }
    }
}

// Модель для прогноза
public class WeatherForecast
{
    public string? City { get; set; }
    public string? Day1 { get; set; }
    public double Temp1 { get; set; }
    public string? Condition1 { get; set; }

    public string? Day2 { get; set; }
    public double Temp2 { get; set; }
    public string? Condition2 { get; set; }

    public string? Day3 { get; set; }
    public double Temp3 { get; set; }
    public string? Condition3 { get; set; }

    public string? Day4 { get; set; }
    public double Temp4 { get; set; }
    public string? Condition4 { get; set; }

    public string? Day5 { get; set; }
    public double Temp5 { get; set; }
    public string? Condition5 { get; set; }
}

