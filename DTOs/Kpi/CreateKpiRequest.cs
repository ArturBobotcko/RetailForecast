namespace RetailForecast.DTOs.Kpi
{
    public record CreateKpiRequest(
        string Name,
        string DataType,
        int DatasetId
    );
}
