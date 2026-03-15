using System;
using System.Collections.Generic;
using System.Text;

namespace RetailForecast.DTOs.Dataset
{
    public record UpdateDatasetRequest(
        string OriginalFileName
    );
}
