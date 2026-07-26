using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockPositionEntityConfiguration : IEntityTypeConfiguration<StockPosition>
{
    public void Configure(EntityTypeBuilder<StockPosition> builder)
    {
        builder.ToTable("stock_position");

        builder.HasKey(p => new { p.Uid, p.PortfolioId, p.StockId, p.ObserveDate });
        builder.HasIndex(p => new { p.Uid, p.PortfolioId, p.StockId, p.ObserveDate });
        builder.HasIndex(p => new { p.Uid, p.PortfolioId, p.ObserveDate });
        builder.HasIndex(p => new { p.Uid, p.ObserveDate });
        builder.HasIndex(p => new { p.ObserveDate });
        builder.HasIndex(p => new { p.IsLatest });

        builder.Property(p => p.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(p => p.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(p => p.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(p => p.AverageCost)
            .HasColumnName("average_cost");

        builder.Property(p => p.UnrealisedAmount)
            .HasColumnName("unrealised_amount")
            .IsRequired();

        builder.Property(p => p.RealisedAmount)
            .HasColumnName("realised_amount")
            .IsRequired();

        builder.Property(p => p.UnrealisedGain)
            .HasColumnName("unrealised_gain")
            .IsRequired();

        builder.Property(p => p.RealisedGain)
            .HasColumnName("realised_gain")
            .IsRequired();

        builder.Property(p => p.RealisedDividend)
            .HasColumnName("realised_dividend")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.UnrealisedCost)
            .HasColumnName("unrealised_cost")
            .IsRequired();

        builder.Property(p => p.RealisedCost)
            .HasColumnName("realised_cost")
            .IsRequired();

        builder.Property(p => p.TotalCost)
            .HasColumnName("total_cost")
            .IsRequired();

        builder.Property(p => p.TotalGain)
            .HasColumnName("total_gain")
            .IsRequired();

        builder.Property(p => p.MarketDate)
            .HasColumnName("market_date");

        builder.Property(p => p.ObserveDate)
            .HasColumnName("observe_date")
            .IsRequired();

        builder.Property(p => p.CurrentGain)
            .HasColumnName("current_gain")
            .IsRequired();

        builder.Property(p => p.PrevStockPrice)
            .HasColumnName("prev_stock_price");

        builder.Property(p => p.IsLatest)
            .HasColumnName("is_latest")
            .IsRequired();

        builder.HasOne(p => p.FkStock)
               .WithMany(s => s.FkStockPositions)
               .HasForeignKey(p => p.StockId)
               .HasPrincipalKey(s => s.StockId);

        builder.HasOne(p => p.FkStockPortfolio)
               .WithMany(port => port.FkStockPositions)
               .HasForeignKey(p => p.PortfolioId)
               .HasPrincipalKey(port => port.PortfolioId);
    }
}
