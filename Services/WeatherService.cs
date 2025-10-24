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
            Console.WriteLine($"📍 Requesting current weather: {url}");

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
                    Console.WriteLine($"✅ Current weather - City: {weatherData.location.name}, Temp: {weatherData.current.temp_c}°C");

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
            Console.WriteLine($"❌ API error: {response.StatusCode}");
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
            // Запрашиваем прогноз на 5 дней
            var url = $"{_baseUrl}forecast.json?key={_apiKey}&q={city}&days=5";
            Console.WriteLine($"📊 Requesting 5-day forecast: {url}");

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"📊 Forecast JSON received");

                var forecastData = JsonSerializer.Deserialize<ForecastApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (forecastData?.forecast?.forecastday != null && forecastData.forecast.forecastday.Length >= 5)
                {
                    var day1 = forecastData.forecast.forecastday[0];
                    var day2 = forecastData.forecast.forecastday[1];
                    var day3 = forecastData.forecast.forecastday[2];
                    var day4 = forecastData.forecast.forecastday[3];
                    var day5 = forecastData.forecast.forecastday[4];

                    //Console.WriteLine($"✅ Forecast parsed for: {forecastData.location?.name}");

                    return new WeatherForecast
                    {
                        City = forecastData.location?.name ?? city,
                        Day1 = DateTime.Parse(day1.date).ToString("dd.MM"),
                        Temp1 = day1.day.maxtemp_c,
                        Condition1 = day1.day.condition?.text,

                        Day2 = DateTime.Parse(day2.date).ToString("dd.MM"),
                        Temp2 = day2.day.maxtemp_c,
                        Condition2 = day2.day.condition?.text,

                        Day3 = DateTime.Parse(day3.date).ToString("dd.MM"),
                        Temp3 = day3.day.maxtemp_c,
                        Condition3 = day3.day.condition?.text,

                        Day4 = DateTime.Parse(day4.date).ToString("dd.MM"),
                        Temp4 = day4.day.maxtemp_c,
                        Condition4 = day4.day.condition?.text,

                        Day5 = DateTime.Parse(day5.date).ToString("dd.MM"),
                        Temp5 = day5.day.maxtemp_c,
                        Condition5 = day5.day.condition?.text
                    };
                }
            }
            Console.WriteLine($"❌ Forecast API error: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Forecast service error: {ex.Message}");
            return null;
        }
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

// Модель для прогноза (используется в Program.cs)
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