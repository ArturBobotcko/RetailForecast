namespace RetailForecast.DTOs.User
{
    public record UserResponse(
        int Id,
        string Email,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
