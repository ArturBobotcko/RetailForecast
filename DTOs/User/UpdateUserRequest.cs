namespace RetailForecast.DTOs.User
{
    public record UpdateUserRequest(
        string? Email = null,
        string? Password = null
    );
}
