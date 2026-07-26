using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class LogStockTagEntityConfiguration : IEntityTypeConfiguration<LogStockTag>
{
    public void Configure(EntityTypeBuilder<LogStockTag> builder)
    {
        builder.ToTable("log_stock_tag");

        builder.HasKey(t => new { t.Uid, t.StockId, t.TagCategory, t.Tag, t.DbUpdatedAt, t.Sta });
        builder.HasIndex(t => new { t.Uid, t.StockId, t.TagCategory, t.Tag, t.DbUpdatedAt, t.Sta });

        builder.Property(t => t.iden)
            .HasColumnName("iden");

        builder.Property(t => t.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40);

        builder.Property(t => t.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20);

        builder.Property(t => t.Percentage)
            .HasColumnName("percentage");

        builder.Property(t => t.TagCategory)
            .HasColumnName("tag_cat");

        builder.Property(t => t.Tag)
            .HasColumnName("tag");

        builder.Property(t => t.Color)
            .HasColumnName("color");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.DbUpdatedAt)
            .HasColumnName("db_updated_at");

        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);
    }
}
