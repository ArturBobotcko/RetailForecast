using RetailForecast.Enums;

namespace RetailForecast.Entities
{
    public class TrainingRun : BaseEntity
    {
        public required string TargetColumn { get; set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? FinishedAt { get; set; }
        public TrainingStatus Status { get; set; } = TrainingStatus.Pending;

        public int DatasetId { get; private set; }
        public Dataset Dataset { get; private set; } = null!;

        public int ModelId { get; private set; }
        public Model Model { get; private set; } = null!;

        public ICollection<Kpi> Features { get; private set; } = [];
        public ICollection<Forecast> Forecasts { get; private set; } = [];
        public ICollection<TrainingMetric> Metrics { get; private set; } = [];
    }
}
