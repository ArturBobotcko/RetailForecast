namespace RetailForecast.DTOs.TrainingRun
{
    public record MlTrainingModelDto(
        int Id,
        string Name,
        string Algorithm
    );
}
