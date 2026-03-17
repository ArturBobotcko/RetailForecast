namespace RetailForecast.DTOs.Kpi
{
    public record UpdateKpiRequest(
        string? Name = null,
        string? DataType = null
    );
}
