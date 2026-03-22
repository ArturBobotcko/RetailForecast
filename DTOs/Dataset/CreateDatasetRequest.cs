namespace RetailForecast.DTOs.Dataset
{
    public record CreateDatasetRequest(
        IFormFile? File,
        string? OriginalFileName,
        string? Description,
        int UserId
    );
}
