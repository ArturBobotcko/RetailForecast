namespace RetailForecast.Entities
{
    public class Dataset : BaseEntity
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string StorageFilePath { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Kpi> Kpis { get; set; } = [];
        public ICollection<TrainingRun> TrainingRuns { get; set; } = [];
    }
}
