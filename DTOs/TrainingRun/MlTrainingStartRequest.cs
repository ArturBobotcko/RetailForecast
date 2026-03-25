namespace RetailForecast.DTOs.TrainingRun
{
    public record MlTrainingStartRequest(
        int TrainingRunId,
        int DatasetId,
        string DownloadUrl,
        string CallbackUrl,
        string TargetColumn,
        List<string> FeatureColumns,
        MlTrainingModelDto Model
    );
}
