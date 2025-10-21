namespace WeatherTelegramBot.Models;

public class WeatherResponse
{
    public string? Name { get; set; }
    public MainData? Main { get; set; }
    public Weather[]? Weather { get; set; }
    public Wind? Wind { get; set; }
}

public class MainData
{
    public double Temp { get; set; }
    public int Humidity { get; set; }
    public double Feels_Like { get; set; }
}

public class Weather
{
    public string? Main { get; set; }
    public string? Description { get; set; }
}

public class Wind
{
    public double Speed { get; set; }
}
