using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockEntityConfiguration : IEntityTypeConfiguration<Stock>
{
       public void Configure(EntityTypeBuilder<Stock> builder)
       {
              builder.ToTable("stock");

              builder.HasKey(s => s.StockId);
              builder.HasIndex(s => s.StockId).IsUnique();

              builder.Property(p => p.iden)
                     .HasColumnName("iden")
                     .ValueGeneratedOnAdd();

              builder.Property(s => s.StockId)
                     .HasColumnName("stock_id")
                     .HasMaxLength(20)
                     .IsRequired();

              builder.Property(s => s.StockName)
                     .HasColumnName("stock_name")
                     .HasMaxLength(80)
                     .IsRequired();

              builder.Property(s => s.Currency)
                     .HasColumnName("currency")
                     .HasMaxLength(3)
                     .IsRequired();

              builder.Property(s => s.AssetClass)
                     .HasColumnName("asset_class")
                     .HasMaxLength(10)
                     .IsRequired();

              builder.Property(s => s.Coupon)
                     .HasColumnName("coupon");

              builder.Property(s => s.CouponFreq)
                     .HasColumnName("coupon_freq");

              builder.Property(s => s.MaturityDate)
                     .HasColumnName("maturity_date");
              
              builder.Property(s => s.FaceValue)
                     .HasColumnName("face_value")
                     .HasPrecision(6, 0);

              builder.Property(t => t.UpdatedAt)
                     .HasColumnName("updated_at");
        
              builder.Property(t => t.CreatedAt)
                     .HasColumnName("created_at");
              
              builder.Property(t => t.Sta)
                     .HasColumnName("sta")
                     .HasMaxLength(5);
              
              builder.Property(s => s.Version)
                     .IsRowVersion();
       }
}
