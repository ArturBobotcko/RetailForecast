using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class ForecastConfiguration : BaseEntityConfiguration<Forecast>
    {
        public override void Configure(EntityTypeBuilder<Forecast> builder)
        {
            base.Configure(builder);

            builder.Property(f => f.Horizon)
            .IsRequired();

            builder.HasOne(f => f.TrainingRun)
                .WithMany(tr => tr.Forecasts)
                .HasForeignKey(f => f.TrainingRunId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
