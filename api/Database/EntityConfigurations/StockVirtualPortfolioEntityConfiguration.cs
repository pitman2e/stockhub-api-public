using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockVirtualPortfolioEntityConfiguration : IEntityTypeConfiguration<StockVirtualPortfolio>
{
    public void Configure(EntityTypeBuilder<StockVirtualPortfolio> builder)
    {
        builder.ToTable("stock_virtual_portfolio");

        builder.HasKey(v => new { v.Uid, v.PortfolioId, v.ChildPortfolioId });
        builder.HasIndex(v => new { v.Uid, v.PortfolioId, v.ChildPortfolioId });

        builder.Property(v => v.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(v => v.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(v => v.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.ChildPortfolioId)
            .HasColumnName("child_portfolio_id")
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);
    }
}
