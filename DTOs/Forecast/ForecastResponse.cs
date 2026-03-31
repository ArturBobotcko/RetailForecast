using RetailForecast.DTOs.TrainingMetric;

namespace RetailForecast.DTOs.Forecast
{
    public record ForecastResponse(
        int Id,
        int Horizon,
        int TrainingRunId,
        string ForecastFrequency,
        string DatasetName,
        string ModelName,
        string TargetColumn,
        string TrainingStatus,
        List<ForecastValueResponse> Values,
        List<TrainingMetricResponse> Metrics,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
