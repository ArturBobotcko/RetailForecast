namespace RetailForecast.DTOs.Kpi
{
    public record KpiResponse(
        int Id,
        string Name,
        string DataType,
        int DatasetId,
        DateTime CreatedAt
    );
}
