namespace RetailForecast.DTOs.Forecast
{
    public record UpdateForecastRequest(
        int? Horizon = null
    );
}
