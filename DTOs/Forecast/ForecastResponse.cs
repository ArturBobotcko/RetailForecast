namespace RetailForecast.DTOs.Forecast
{
    public record ForecastResponse(
        int Id,
        int Horizon,
        int TrainingRunId,
        string DatasetName,
        string ModelName,
        string TargetColumn,
        string TrainingStatus,
        List<ForecastValueResponse> Values,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
