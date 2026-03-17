namespace RetailForecast.DTOs.Model
{
    public record UpdateModelRequest(
        string Name,
        string Algorithm,
        string Description
    );
}
