namespace StockHub.Controllers.Tag;
public record TagCsvPostDto
{
    public string Category { get; init; }
    public string Csv { get; init; }
}