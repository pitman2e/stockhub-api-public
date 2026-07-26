using System;
using System.Text.Json;

namespace StockHub.Errors;

public class SHArgumentException : ArgumentException
{
    public string? FieldName { get; private set; } = null;
    
    public SHArgumentException(string message, string? fieldName = null) : base(message)
    {
        FieldName = fieldName != null 
            ? JsonNamingPolicy.CamelCase.ConvertName(fieldName) 
            : null;
    }
}