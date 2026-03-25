namespace RetailForecast.DTOs.Dataset
{
    public record DatasetPreviewResponse(
        string OriginalFileName,
        List<string> Columns,
        List<Dictionary<string, string>> Rows,
        int TotalRows,
        int CurrentPage,
        int PageSize,
        bool Preview
    );
}
