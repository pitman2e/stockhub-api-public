using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockPriceEntityConfiguration : IEntityTypeConfiguration<StockPrice>
{
    public void Configure(EntityTypeBuilder<StockPrice> builder)
    {
        builder.ToTable("stock_price");

        builder.HasKey(p => new { p.StockId, p.MarketDate });
        builder.HasIndex(p => new { p.StockId, p.MarketDate }).IsUnique();
        builder.HasIndex(p => new { p.StockId, p.MarketDate, p.IsFinalised }).IsUnique();

        builder.Property(p => p.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(p => p.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.MarketDate)
            .HasColumnName("market_date")
            .IsRequired();

        builder.Property(p => p.OpenPrice)
            .HasColumnName("open_price");

        builder.Property(p => p.DayHigh)
            .HasColumnName("day_high");

        builder.Property(p => p.DayLow)
            .HasColumnName("day_low");

        builder.Property(p => p.ClosePrice)
            .HasColumnName("close_price")
            .IsRequired();

        builder.Property(p => p.ClosePriceAdj)
            .HasColumnName("close_price_adj");

        builder.Property(p => p.Volume)
            .HasColumnName("volume");

        builder.Property(p => p.IsFinalised)
            .HasColumnName("is_finalised")
            .IsRequired();

        builder.HasOne(p => p.FkStock)
               .WithMany(s => s.FkStockPrices)
               .HasForeignKey(p => p.StockId);
    }
}
