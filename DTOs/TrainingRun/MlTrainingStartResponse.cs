namespace RetailForecast.DTOs.TrainingRun
{
    public record MlTrainingStartResponse(
        string? ExternalJobId,
        string? Status,
        string? Message
    );
}
