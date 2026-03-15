namespace RetailForecast.Entities
{
    public class TrainingMetric : BaseEntity
    {
        public required string Name { get; set; }
        public double Value { get; set; }

        public int TrainingRunId { get; private set; }
        public TrainingRun TrainingRun { get; private set; } = null!;
    }
}
