namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunResponse(
        int Id,
        string TargetColumn,
        DateTime StartedAt,
        DateTime? FinishedAt,
        string Status,
        int DatasetId,
        int ModelId,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
