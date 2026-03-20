namespace RetailForecast.DTOs.User
{
    public record LoginRequest(
        string Email,
        string Password
    );
}
