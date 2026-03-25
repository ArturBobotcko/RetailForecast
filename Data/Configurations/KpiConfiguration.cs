using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class KpiConfiguration : BaseEntityConfiguration<Kpi>
    {
        public override void Configure(EntityTypeBuilder<Kpi> builder)
        {
            base.Configure(builder);

            builder.Property(k => k.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne(k => k.Dataset)
                .WithMany(d => d.Kpis)
                .HasForeignKey(k => k.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
