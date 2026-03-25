using RetailForecast.Enums;

namespace RetailForecast.Entities
{
    public class TrainingRun : BaseEntity
    {
        public required string TargetColumn { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
        public TrainingStatus Status { get; set; } = TrainingStatus.Pending;
        public string? ExternalJobId { get; set; }
        public string? ErrorMessage { get; set; }

        public int DatasetId { get; set; }
        public Dataset Dataset { get; private set; } = null!;

        public int ModelId { get; set; }
        public Model Model { get; private set; } = null!;

        public ICollection<Kpi> Features { get; set; } = [];
        public ICollection<Forecast> Forecasts { get; private set; } = [];
        public ICollection<TrainingMetric> Metrics { get; private set; } = [];
    }
}
