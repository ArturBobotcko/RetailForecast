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
        List<ForecastHistoryPointResponse> HistoryValues,
        List<TrainingMetricResponse> Metrics,
        ForecastDataQualityResponse? DataQuality,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
