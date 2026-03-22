namespace RetailForecast.DTOs.Dataset
{
    public record UpdateDatasetRequest(
        string? OriginalFileName = null,
        string? Description = null
    );
}
