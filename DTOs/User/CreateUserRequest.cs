namespace RetailForecast.DTOs.User
{
    public record CreateUserRequest(
        string Email,
        string PasswordHash
    );
}
