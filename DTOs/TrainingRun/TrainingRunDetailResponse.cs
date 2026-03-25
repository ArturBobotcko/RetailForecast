using RetailForecast.DTOs.TrainingMetric;

namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunDetailResponse(
        int Id,
        string Status,
        string TargetColumn,
        int DatasetId,
        string DatasetName,
        int ModelId,
        string ModelName,
        string? ExternalJobId,
        string? ErrorMessage,
        List<string> FeatureColumns,
        List<TrainingMetricResponse> Metrics,
        DateTime StartedAt,
        DateTime? FinishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
