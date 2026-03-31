namespace RetailForecast.DTOs.TrainingRun
{
    public record MlTrainingStartRequest(
        int TrainingRunId,
        int DatasetId,
        string DownloadUrl,
        string CallbackUrl,
        int ForecastHorizon,
        string ForecastFrequency,
        string TargetColumn,
        List<string> FeatureColumns,
        MlTrainingModelDto Model
    );
}
