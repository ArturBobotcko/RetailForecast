namespace RetailForecast.DTOs.Forecast
{
    public record ForecastHistoryPointResponse(
        DateTime Timestamp,
        double Value
    );
}
