namespace RetailForecast.DTOs.Model
{
    public record UpdateModelRequest(
        string? Name = null,
        string? Algorithm = null,
        string? Description = null
    );
}
