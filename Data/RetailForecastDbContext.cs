using Microsoft.EntityFrameworkCore;
using RetailForecast.Entities;

namespace RetailForecast.Data
{
    public class RetailForecastDbContext : DbContext
    {
        public RetailForecastDbContext(DbContextOptions<RetailForecastDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Dataset> Datasets => Set<Dataset>();
        public DbSet<Kpi> Kpis => Set<Kpi>();
        public DbSet<Model> Models => Set<Model>();
        public DbSet<TrainingRun> TrainingRuns => Set<TrainingRun>();
        public DbSet<TrainingMetric> TrainingMetrics => Set<TrainingMetric>();
        public DbSet<Forecast> Forecasts => Set<Forecast>();
        public DbSet<ForecastValue> ForecastValues => Set<ForecastValue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetailForecastDbContext).Assembly);
        }
    }
}
