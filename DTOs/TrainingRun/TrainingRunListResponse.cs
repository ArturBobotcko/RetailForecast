namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunListResponse(
        int Id,
        string Status,
        string TargetColumn,
        int DatasetId,
        string DatasetName,
        int ModelId,
        string ModelName,
        DateTime StartedAt,
        DateTime? FinishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
