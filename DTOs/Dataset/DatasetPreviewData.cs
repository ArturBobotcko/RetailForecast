namespace RetailForecast.DTOs.Dataset
{
    public record DatasetPreviewData(
        List<string> Columns,
        List<Dictionary<string, string>> Rows,
        int TotalRows
    );
}
