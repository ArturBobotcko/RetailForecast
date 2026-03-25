namespace RetailForecast.DTOs.TrainingRun
{
    public record StartTrainingRunResponse(
        int Id,
        string Status,
        string? ExternalJobId
    );
}
