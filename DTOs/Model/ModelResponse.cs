namespace RetailForecast.DTOs.Model
{
    public record ModelResponse(
        int Id,
        string Name,
        string Algorithm,
        string? Description,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
