namespace RetailForecast.Entities
{
    public class Dataset : BaseEntity
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string StorageFileName { get; set; } = string.Empty;  // Имя файла с разрешением конфликтов
        public string StorageFilePath { get; set; } = string.Empty;   // Полный путь на диске
        public long FileSizeBytes { get; set; }                       // Размер в байтах
        public string FileExtension { get; set; } = string.Empty;     // .csv, .xlsx и т.д.
        public string? Description { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Kpi> Kpis { get; set; } = [];
        public ICollection<TrainingRun> TrainingRuns { get; set; } = [];
    }
}

