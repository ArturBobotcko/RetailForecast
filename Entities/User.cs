namespace RetailForecast.Entities
{
    public class User : BaseEntity
    {
        public required string Email { get; set; }
        public string? PasswordHash { get; set; }

        public ICollection<Dataset> Datasets { get; private set; } = [];
    }
}
