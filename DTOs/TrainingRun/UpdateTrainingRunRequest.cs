namespace RetailForecast.DTOs.TrainingRun
{
    public record UpdateTrainingRunRequest(
        string? TargetColumn = null,
        int? ForecastHorizon = null,
        string? ForecastFrequency = null,
        DateTime? FinishedAt = null,
        string? Status = null
    );
}
