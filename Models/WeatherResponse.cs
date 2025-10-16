namespace WeatherTelegramBot.Models;

public class WeatherResponse
{
    public MainData? Main { get; set; }      // ← добавить ?
    public Weather[]? Weather { get; set; }  // ← добавить ?
    public Wind? Wind { get; set; }          // ← добавить ?
    public string? Name { get; set; }        // ← добавить ?
}

public class MainData
{
    public double Temp { get; set; }
    public int Humidity { get; set; }
    public double Feels_Like { get; set; }
}

public class Weather
{
    public string? Main { get; set; }        // ← добавить ?
    public string? Description { get; set; } // ← добавить ?
}

public class Wind
{
    public double Speed { get; set; }
}
