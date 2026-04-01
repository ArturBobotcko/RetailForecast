namespace RetailForecast.DTOs.Forecast
{
    public record ForecastDataQualityResponse(
        int RowCount,
        int NumericColumnCount,
        int MissingValueCount,
        int StrongCorrelationCount,
        string? TimeColumn,
        string Summary
    );
}
