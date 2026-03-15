using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class TrainingMetricConfiguration : BaseEntityConfiguration<TrainingMetric>
    {
        public override void Configure(EntityTypeBuilder<TrainingMetric> builder)
        {
            base.Configure(builder);

            builder.Property(tm => tm.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(tm => tm.Value)
                .IsRequired();

            builder.HasOne(tm => tm.TrainingRun)
                .WithMany(tr => tr.Metrics)
                .HasForeignKey(tm => tm.TrainingRunId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
