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
        List<string> FeatureColumns,
        DateTime StartedAt,
        DateTime? FinishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
