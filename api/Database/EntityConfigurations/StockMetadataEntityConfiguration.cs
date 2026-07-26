using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockMetadataEntityConfiguration : IEntityTypeConfiguration<StockMetadata>
{
       public void Configure(EntityTypeBuilder<StockMetadata> builder)
       {
              builder.ToTable("stock_metadata");

              builder.HasKey(s => s.StockId);
              builder.HasIndex(s => s.StockId).IsUnique();

              builder.Property(s => s.StockId)
                     .HasColumnName("stock_id")
                     .HasMaxLength(20)
                     .IsRequired();

              builder.Property(s => s.DivCrawlDate)
                     .HasColumnName("div_crawl_date");

              builder.Property(s => s.PriceCrawlDate)
                     .HasColumnName("price_crawl_date");
              
              builder.Property(s => s.PriceMinDate)
                     .HasColumnName("price_min_date");

              builder.Property(s => s.PriceMaxDate)
                     .HasColumnName("price_max_date");

              builder.Property(s => s.TxMinDate)
                     .HasColumnName("tx_min_date");

              builder.Property(s => s.TxMaxDate)
                     .HasColumnName("tx_max_date");

              builder.Property(s => s.Version)
                     .IsRowVersion();

              builder.HasOne(m => m.FkStock)
                     .WithOne(s => s.FkStockMeta)
                     .HasForeignKey<StockMetadata>(m => m.StockId);
       }
}
