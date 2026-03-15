using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class DatasetConfiguration : BaseEntityConfiguration<Dataset>
    {
        public override void Configure(EntityTypeBuilder<Dataset> builder)
        {
            base.Configure(builder);

            builder.Property(d => d.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.StorageFilePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(d => d.User)
                .WithMany(u => u.Datasets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
