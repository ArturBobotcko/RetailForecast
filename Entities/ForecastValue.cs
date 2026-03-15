namespace RetailForecast.Entities
{
    public class ForecastValue : BaseEntity
    {
        public DateTime Timestamp { get; init; }
        public double Value { get; init; }

        public int ForecastId { get; private set; }
        public Forecast Forecast { get; private set; } = null!;
    }
}
