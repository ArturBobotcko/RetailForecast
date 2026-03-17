namespace RetailForecast.DTOs.User
{
    public record UpdateUserRequest(
        string Email,
        string PasswordHash
    );
}
