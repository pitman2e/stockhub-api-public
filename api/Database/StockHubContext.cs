using Microsoft.EntityFrameworkCore;

namespace StockHub.Database;

public class StockHubContext : DbContext
{
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<StockDividend> StockDividends { get; set; }
    public DbSet<StockPrice> StockPrices { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockRealisedScrip> StockRealisedScrips { get; set; }
    public DbSet<StockPortfolio> StockPortfolios { get; set; }
    public DbSet<StockPosition> StockPositions { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<StockVirtualPortfolio> StockVirtualPortfolios { get; set; }
    public DbSet<StockWatchlist> StockWatchlists { get; set; }
    public DbSet<StockUser> Users { get; set; }
    public DbSet<StockTag> StockTags { get; set; }

    private string _initConnectionString { get; }

    public StockHubContext(DbContextOptions<StockHubContext> context) : base(context)
    {

    }

    public StockHubContext(string connectionString) : base()
    {
        _initConnectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(_initConnectionString))
        {
            optionsBuilder.UseNpgsql(_initConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockHubContext).Assembly);
    }
}