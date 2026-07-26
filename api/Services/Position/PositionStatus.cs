namespace StockHub.Services.Position;

public partial class PositionValueService
{
    public enum PositionStatus
    {
        Any,
        Open,
        Closed,
        OpenOrChanged,
    }
}