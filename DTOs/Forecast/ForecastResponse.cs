namespace RetailForecast.DTOs.Forecast
{
    public record ForecastResponse(
        int Id,
        int Horizon,
        int TrainingRunId,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
