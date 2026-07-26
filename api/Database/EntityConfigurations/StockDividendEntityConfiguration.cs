using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockDividendEntityConfiguration : IEntityTypeConfiguration<StockDividend>
{
    public void Configure(EntityTypeBuilder<StockDividend> builder)
    {
        builder.ToTable("stock_dividend");

        builder.HasKey(d => new { d.StockId, d.DividendType, d.DistributionType, d.PayableDate });
        builder.HasIndex(d => new { d.StockId, d.PayableDate });

        builder.Property(d => d.DividendId)
            .HasColumnName("dividend_id")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(d => d.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.AnnounceDate)
            .HasColumnName("announce_date")
            .IsRequired();

        builder.Property(d => d.DividendEvent)
            .HasColumnName("dividend_event")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.DividendType)
            .HasColumnName("dividend_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.DistributionType)
            .HasColumnName("distribution_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(d => d.ScripPrice)
            .HasColumnName("scrip_price");

        builder.Property(d => d.ExDate)
            .HasColumnName("ex_date")
            .IsRequired();

        builder.Property(d => d.PayableDate)
            .HasColumnName("payable_date")
            .IsRequired();

        builder.Property(d => d.ScripPerCount)
            .HasColumnName("scrip_per_count");

        builder.Property(d => d.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(d => d.PrevAmount)
            .HasColumnName("prev_amount");

        builder.Property(d => d.AmountAdjPercentage)
            .HasColumnName("amount_adj_percentage");

        builder.Property(d => d.Version)
            .IsRowVersion();

        builder.HasOne(d => d.FkCurrency)
               .WithMany(c => c.FkStockDividends)
               .HasForeignKey(d => d.Currency)
               .HasPrincipalKey(c => c.CurrencyId);

        builder.HasOne(d => d.FkStock)
               .WithMany(s => s.FkStockDividends)
               .HasForeignKey(d => d.StockId)
               .HasPrincipalKey(s => s.StockId);
    }
}
