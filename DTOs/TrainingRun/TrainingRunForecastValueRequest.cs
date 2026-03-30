namespace RetailForecast.DTOs.TrainingRun
{
    public record TrainingRunForecastValueRequest(
        DateTime Timestamp,
        double Value
    );
}
