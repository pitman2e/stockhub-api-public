using System;

namespace StockHub.Errors;

public class SHArgumentException : ArgumentException
{
    public string? FieldName { get; private set; } = null;
    
    public SHArgumentException(string message, string? fieldName = null) : base(message)
    {
        FieldName = fieldName;
    }
}