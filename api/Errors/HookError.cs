namespace StockHub.Errors;

public class HookError
{
    public HookError(string fieldName, string message)
    {
        FieldName = fieldName;
        Message = message;
    }

    public string FieldName { get; set; }
    public string Message { get; set; }
}