using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class CurrencyEntityConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currency");

        builder.HasKey(c => new { c.CurrencyId });
        builder.HasIndex(c => new { c.CurrencyId });

        builder.Property(c => c.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CurrencyId)
            .IsRequired()
            .HasMaxLength(3)
            .HasColumnName("currency_id");

        builder.Property(c => c.CurrencyName)
            .HasMaxLength(40)
            .HasColumnName("currency_name");

        builder.Property(c => c.ToUsdRate)
            .HasColumnName("to_usd_rate");

        builder.HasMany(c => c.FkStockPortfolios)
            .WithOne(p => p.FkDefaultCurrency)
            .HasForeignKey(p => p.DefaultCurrency)
            .HasPrincipalKey(c => c.CurrencyId);

        builder.HasMany(c => c.FkStockDividends)
            .WithOne(d => d.FkCurrency)
            .HasForeignKey(d => d.Currency)
            .HasPrincipalKey(c => c.CurrencyId);
    }
}