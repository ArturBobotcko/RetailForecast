using System;
using System.Collections.Generic;
using System.Text;

namespace RetailForecast.DTOs.Dataset
{
    public record CreateDatasetRequest(
        string OriginalFileName,
        int UserId
    );
}
