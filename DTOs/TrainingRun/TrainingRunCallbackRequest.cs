namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunCallbackRequest(
        string Status,
        List<MlMetricValueRequest>? Metrics,
        string? Error,
        string? ExternalJobId
    );
}
