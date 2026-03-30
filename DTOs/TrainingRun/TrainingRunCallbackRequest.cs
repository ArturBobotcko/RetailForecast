namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunCallbackRequest(
        string Status,
        List<MlMetricValueRequest>? Metrics,
        List<TrainingRunForecastValueRequest>? Forecast,
        string? Error,
        string? ExternalJobId
    );
}
