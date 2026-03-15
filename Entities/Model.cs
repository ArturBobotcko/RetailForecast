namespace RetailForecast.Entities
{
    public class Model : BaseEntity
    {
        public required string Name { get; set; }
        public required string Algorithm { get; set; }
        public string? Description { get; set; }

        public ICollection<TrainingRun> TrainingRuns { get; private set; } = [];
    }
}
