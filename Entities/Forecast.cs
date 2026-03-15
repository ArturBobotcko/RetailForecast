namespace RetailForecast.Entities
{
    public class Forecast : BaseEntity
    {
        public int Horizon { get; set; }

        public int TrainingRunId { get; private set; }
        public TrainingRun TrainingRun { get; private set; } = null!;

        public ICollection<ForecastValue> ForecastValues { get; private set; } = [];
    }
}
