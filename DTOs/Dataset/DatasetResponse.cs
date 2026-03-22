using System;

namespace RetailForecast.DTOs.Dataset
{
    public record DatasetResponse(
        int Id,
        string OriginalFileName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int UserId
    );
}
