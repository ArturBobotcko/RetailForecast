namespace RetailForecast.DTOs.TrainingRun
{
    public record CreateTrainingRunRequest(
        string TargetColumn,
        int DatasetId,
        int ModelId,
        List<string> FeatureColumns
    );
}
