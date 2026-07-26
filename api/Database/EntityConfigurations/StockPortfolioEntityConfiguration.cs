using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockPortfolioEntityConfiguration : IEntityTypeConfiguration<StockPortfolio>
{
    public void Configure(EntityTypeBuilder<StockPortfolio> builder)
    {
        builder.ToTable("stock_portfolio");

        builder.HasKey(p => new { p.Uid, p.PortfolioId });
        builder.HasIndex(p => new { p.Uid, p.PortfolioId });

        builder.Property(p => p.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(p => p.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(p => p.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(p => p.DefaultCurrency)
            .HasColumnName("default_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.IsExcludedFromSummary)
            .HasColumnName("is_ex_summary")
            .IsRequired();

        builder.Property(p => p.IsVirtual)
            .HasColumnName("is_virtual")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);
        
        builder.Property(p => p.Version)
            .IsRowVersion();

        builder.HasOne(p => p.FkDefaultCurrency)
            .WithMany(c => c.FkStockPortfolios)
            .HasForeignKey(p => p.DefaultCurrency)
            .HasPrincipalKey(c => c.CurrencyId);

        builder.HasMany(p => p.FkStockVirtualPortfolios)
            .WithOne(p => p.FkPortfolio)
            .HasPrincipalKey(p => new { p.Uid, p.PortfolioId })
            .HasForeignKey(p => new { p.Uid, p.PortfolioId });

        builder.HasMany(p => p.FkStockVirtualChildPortfolios)
            .WithOne(p => p.FkChildPortfolio)
            .HasPrincipalKey(p => new { p.Uid, p.PortfolioId })
            .HasForeignKey(p => new { p.Uid, p.ChildPortfolioId });
    }
}
