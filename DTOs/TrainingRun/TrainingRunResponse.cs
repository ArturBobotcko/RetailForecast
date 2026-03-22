namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunResponse(
        int Id,
        string TargetColumn,
        DateTime StartedAt,
        DateTime? FinishedAt,
        string Status,
        int DatasetId,
        string DatasetName,
        int ModelId,
        string ModelName,
        List<string> FeatureColumns,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
