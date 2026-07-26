namespace StockHub.Controllers.Transaction;

public record TransactionPutDto : TransactionModifyDto
{
    public uint Version { get; init; }
    public int Iden { get; init; }
};