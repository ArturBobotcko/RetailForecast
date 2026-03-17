namespace RetailForecast.DTOs.Model
{
    public record CreateModelRequest(
        string Name,
        string Algorithm,
        string? Description
    );
}
