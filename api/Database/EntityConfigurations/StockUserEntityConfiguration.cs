using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StockHub.Database.EntityConfigurations;

public class StockUserEntityConfiguration : IEntityTypeConfiguration<StockUser>
{
    public void Configure(EntityTypeBuilder<StockUser> builder)
    {
        builder.ToTable("stock_user");

        builder.HasKey(u => new { u.Uid });
        builder.HasIndex(u => new { u.Uid });

        builder.Property(u => u.iden)
            .HasColumnName("iden")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Uid)
            .HasColumnName("uid")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(u => u.LastBeat)
            .HasColumnName("last_beat");
    }
}
