namespace RetailForecast.DTOs.Dataset
{
    public record DatasetFeatureAnalysisResponse(
        string FileName,
        List<string> Columns,
        List<string> NumericColumns,
        Dictionary<string, Dictionary<string, double?>> CorrelationMatrix,
        List<DatasetFeatureCorrelationResponse> StrongCorrelations,
        int RowCount,
        int MissingValueCount,
        string Summary
    );
}
