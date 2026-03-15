using System;
using System.Collections.Generic;
using System.Text;

namespace RetailForecast.DTOs.Dataset
{
    public record DatasetResponse(
        int Id,
        string OriginalFileName,
        DateTime UploadedAt,
        int UserId
    );
}
