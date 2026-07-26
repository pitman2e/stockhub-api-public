using System;
using System.Collections.Generic;
using StockHub.Errors;

namespace StockHub.Models;

public class ApiActionResult<T>
{
    private bool _isSuccess = true;
    public bool IsSuccess
    {
        get => _isSuccess && HookErrors.Count == 0;
        set => _isSuccess = value;
    }

    public string Message { get; set; } = "";
    public List<HookError> HookErrors { get; init; } = [];
    public long Timestamp { get; init; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public T Payload { get; set; }
}