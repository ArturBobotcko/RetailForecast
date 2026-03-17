namespace RetailForecast.Entities
{
    public class Kpi : BaseEntity
    {
        public required string Name { get; set; }
        public required string DataType { get; set; }

        public int DatasetId { get; set; }
        public Dataset Dataset { get; private set; } = null!;

        public ICollection<TrainingRun> TrainingRuns { get; private set; } = [];
    }
}
