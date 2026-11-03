using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class WeatherHourConfiguration : IEntityTypeConfiguration<WeatherHour>
{
    public void Configure(EntityTypeBuilder<WeatherHour> builder)
    {
        builder.ToTable("weather_hours");
        builder.HasKey(x => new { x.FieldId, x.Date, x.Hour });
        builder.Property(x => x.FieldId).HasConversion(x => x.Value, x => new FieldId(x));
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Cascade);
    }
}
