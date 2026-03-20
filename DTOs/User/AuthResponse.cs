namespace RetailForecast.DTOs.User
{
    public record AuthResponse(
        int Id,
        string Email,
        string Token
    );
}
