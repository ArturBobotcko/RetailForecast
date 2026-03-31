namespace RetailForecast.DTOs.TrainingRun
{
    public record CreateTrainingRunRequest(
        string TargetColumn,
        int ForecastHorizon,
        string ForecastFrequency,
        int DatasetId,
        int ModelId,
        List<string> FeatureColumns
    );
}
