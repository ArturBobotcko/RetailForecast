namespace RetailForecast.DTOs.TrainingMetric
{
    public record TrainingMetricResponse(
        int Id,
        string Name,
        double Value,
        int TrainingRunId,
        DateTime CreatedAt
    );
}
