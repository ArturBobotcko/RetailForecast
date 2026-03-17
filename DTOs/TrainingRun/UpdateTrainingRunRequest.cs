namespace RetailForecast.DTOs.TrainingRun
{
    public record UpdateTrainingRunRequest(
        string? TargetColumn = null,
        DateTime? FinishedAt = null,
        string? Status = null
    );
}
