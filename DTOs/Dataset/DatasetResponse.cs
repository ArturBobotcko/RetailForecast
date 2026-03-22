using System;

namespace RetailForecast.DTOs.Dataset
{
    public record DatasetResponse(
        int Id,
        string OriginalFileName,
        string StorageFileName,
        long FileSizeBytes,
        string FileExtension,
        string? Description,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int UserId
    );
}
