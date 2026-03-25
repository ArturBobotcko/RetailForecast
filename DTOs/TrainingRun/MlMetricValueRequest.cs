namespace RetailForecast.DTOs.TrainingRun
{
    public record MlMetricValueRequest(
        string Name,
        double Value
    );
}
