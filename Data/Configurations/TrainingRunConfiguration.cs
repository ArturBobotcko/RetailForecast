using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailForecast.Entities;

namespace RetailForecast.Data.Configurations
{
    public class TrainingRunConfiguration : BaseEntityConfiguration<TrainingRun>
    {
        public override void Configure(EntityTypeBuilder<TrainingRun> builder)
        {
            base.Configure(builder);

            builder.Property(tr => tr.TargetColumn)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(tr => tr.StartedAt)
                .IsRequired();

            builder.Property(tr => tr.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(tr => tr.ExternalJobId)
                .HasMaxLength(255);

            builder.Property(tr => tr.ErrorMessage)
                .HasMaxLength(4000);

            builder.HasOne(tr => tr.Model)
                .WithMany(m => m.TrainingRuns)
                .HasForeignKey(tr => tr.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(tr => tr.Dataset)
                .WithMany(d => d.TrainingRuns)
                .HasForeignKey(tr => tr.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(tr => tr.Features)
                .WithMany(k => k.TrainingRuns)
                .UsingEntity(j => j.ToTable("training_run_features"));
        }
    }
}
