namespace RetailForecast.DTOs.TrainingMetric
{
    public record CreateTrainingMetricRequest(
        string Name,
        double Value,
        int TrainingRunId
    );
}
