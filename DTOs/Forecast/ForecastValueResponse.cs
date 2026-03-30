namespace RetailForecast.DTOs.Forecast
{
    public record ForecastValueResponse(
        DateTime Timestamp,
        double Value
    );
}
