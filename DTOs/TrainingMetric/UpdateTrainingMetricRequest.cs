namespace RetailForecast.DTOs.TrainingMetric
{
    public record UpdateTrainingMetricRequest(
        string? Name = null,
        double? Value = null
    );
}
