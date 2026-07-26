using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockTransactionEntityConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transaction");

        builder.HasKey(t => new { t.Uid, t.iden, t.PortfolioId });
        builder.HasIndex(t => new { t.Uid, t.PortfolioId, t.StockId, t.TxDate });

        builder.Property(t => t.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(t => t.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(t => t.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.UnitAmt)
            .HasColumnName("unit_amt")
            .IsRequired();

        builder.Property(t => t.TxCount)
            .HasColumnName("count")
            .IsRequired();

        builder.Property(t => t.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.TxDate)
            .HasColumnName("tx_date")
            .IsRequired();

        builder.Property(t => t.TranType)
            .HasColumnName("tran_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.HandlingFee)
            .HasColumnName("handling_fee");

        builder.Property(t => t.AccruedInterest)
            .HasColumnName("accrued_interest");

        builder.Property(t => t.Tax)
            .HasColumnName("tax");

        builder.Property(t => t.YTM)
            .HasColumnName("ytm");

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Comment)
            .HasColumnName("comment");

        builder.Property(t => t.isTransfer)
            .HasColumnName("is_transfer");
        
        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);

        builder.Property(t => t.Version)
            .IsRowVersion();

        builder.HasOne(t => t.FkStock)
               .WithMany(s => s.FkStockTransactions)
               .HasForeignKey(t => t.StockId)
               .HasPrincipalKey(s => s.StockId);

        builder.HasOne(t => t.FkStockPortfolio)
               .WithMany(p => p.FkStockTransactions)
               .HasForeignKey(t => new { t.Uid, t.PortfolioId })
               .HasPrincipalKey(p => new { p.Uid, p.PortfolioId });
    }
}
