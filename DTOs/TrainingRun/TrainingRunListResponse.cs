namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunListResponse(
        int Id,
        string Status,
        string TargetColumn,
        int ForecastHorizon,
        string ForecastFrequency,
        int DatasetId,
        string DatasetName,
        int ModelId,
        string ModelName,
        string? ExternalJobId,
        DateTime StartedAt,
        DateTime? FinishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
