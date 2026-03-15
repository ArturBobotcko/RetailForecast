using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class ForecastValueConfiguration : BaseEntityConfiguration<ForecastValue>
    {
        public override void Configure(EntityTypeBuilder<ForecastValue> builder)
        {
            base.Configure(builder);

            builder.Property(fv => fv.Timestamp)
                .IsRequired();

            builder.Property(fv => fv.Value)
                .IsRequired();

            builder.HasOne(fv => fv.Forecast)
                .WithMany(f => f.ForecastValues)
                .HasForeignKey(fv => fv.ForecastId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
