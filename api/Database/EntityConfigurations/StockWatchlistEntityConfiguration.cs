using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockWatchlistEntityConfiguration : IEntityTypeConfiguration<StockWatchlist>
{
    public void Configure(EntityTypeBuilder<StockWatchlist> builder)
    {
        builder.ToTable("stock_watchlist");

        builder.HasKey(w => new { w.Uid, w.StockId });
        builder.HasIndex(w => new { w.Uid, w.StockId });

        builder.Property(w => w.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd();

        builder.Property(w => w.StockId)
            .HasColumnName("stock_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(w => w.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();
        
        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(t => t.Sta)
            .HasColumnName("sta")
            .HasMaxLength(5);

        builder.HasOne(w => w.FkStock)
               .WithMany(s => s.FkStockWatchlists);
    }
}
