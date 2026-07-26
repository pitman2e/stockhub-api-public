using System;

namespace StockHub.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset ToUTCDateOnly(this DateTimeOffset dt)
    {
        var du = dt.ToUniversalTime();
        return new DateTimeOffset(du.Year, du.Month, du.Day, 0, 0, 0, 0, new TimeSpan(0, 0, 0));
    }
    
    public static DateOnly ToUtcThenDateOnly(this DateTimeOffset dt)
    {
        var du = dt.ToUniversalTime();
        return new DateOnly(du.Year, du.Month, du.Day);
    }

    public static DateOnly ToDateOnly(this DateTimeOffset dt)
    {
        return new DateOnly(dt.Year, dt.Month, dt.Day);
    }
    
    /// <summary>
    /// Get a DateTimeOffset object with designated offset, without changing the DateTime part
    ///
    /// Use case:
    ///     Parsing DateTime string to DateTimeOffset, .NET will convert the object to system local timezone.
    ///     Use this extension to counteract the local timezone 
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static DateTimeOffset GetAsOffset(this DateTimeOffset dt, int offset)
    {
        return new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, new TimeSpan(offset, 0, 0));
    }

    public static DateTimeOffset ToOffset(this DateTimeOffset dt, int offset)
    {
        return dt.ToOffset(new TimeSpan(offset, 0, 0));
    }
}