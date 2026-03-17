namespace RetailForecast.DTOs.Forecast
{
    public record CreateForecastRequest(
        int Horizon,
        int TrainingRunId
    );
}
