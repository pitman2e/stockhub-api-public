using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockTagEntityConfiguration : IEntityTypeConfiguration<StockTag>
{
    public void Configure(EntityTypeBuilder<StockTag> builder)
    {
        builder.ToTable("stock_tag");

        builder.HasKey(t => new { t.Uid, t.StockId, t.TagCategory, t.Tag });
        builder.HasIndex(t => new { t.Uid, t.StockId, t.TagCategory, t.Tag });

        builder.Property(t => t.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(t => t.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(t => t.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Percentage)
            .HasColumnName("percentage")
            .IsRequired();

        builder.Property(t => t.TagCategory)
            .HasColumnName("tag_cat")
            .IsRequired();

        builder.Property(t => t.Tag)
            .HasColumnName("tag")
            .IsRequired();

        builder.Property(t => t.Color)
            .HasColumnName("color");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);

        builder.HasOne(t => t.FkStock)
               .WithMany(s => s.FkStockTags)
               .HasForeignKey(t => t.StockId)
               .HasPrincipalKey(s => s.StockId);
    }
}
