using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockRealisedScripEntityConfiguration : IEntityTypeConfiguration<StockRealisedScrip>
{
    public void Configure(EntityTypeBuilder<StockRealisedScrip> builder)
    {
        builder.ToTable("stock_realised_scrip");

        builder.HasKey(r => new { r.Uid, r.PortfolioId, r.StockId, r.DividendType, r.DistributionType, r.PayableDate });
        builder.HasIndex(r => new { r.Uid, r.PortfolioId, r.StockId, r.PayableDate });

        builder.Property(r => r.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(r => r.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(r => r.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.DividendType)
            .HasColumnName("dividend_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.DistributionType)
            .HasColumnName("distribution_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.PayableDate)
            .HasColumnName("payable_date")
            .IsRequired();

        builder.Property(r => r.ScripReceived)
            .HasColumnName("scrip_received")
            .IsRequired();

        builder.Property(r => r.ReinvestPrice)
            .HasColumnName("reinv_price");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);
        
        builder.Property(r => r.Version)
            .IsRowVersion();

        builder.HasOne(s => s.FkStockDividend)
            .WithMany(s => s.FkStockRealisedScrips)
            .HasPrincipalKey(s => new { s.StockId, s.DividendType, s.DistributionType, s.PayableDate })
            .HasForeignKey(s => new { s.StockId, s.DividendType, s.DistributionType, s.PayableDate });

        builder.HasOne(s => s.FkStockPortfolio)
            .WithMany(s => s.FkStockRealisedScrips)
            .HasPrincipalKey(s => new { s.PortfolioId })
            .HasForeignKey(s => new { s.PortfolioId });
    }
}
