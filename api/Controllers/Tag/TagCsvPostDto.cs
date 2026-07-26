namespace StockHub.Controllers.Tag;
public record TagCsvPostDto
{
    public string category { get; init; }
    public string csv { get; init; }
}