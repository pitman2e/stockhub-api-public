using System;
using Xunit;
using StockHub.Extensions;

namespace UnitTests;

public class DateTimeOffsetExtensionsTest
{
    [Fact]
    public void ToUTCDateOnly_SameDay()
    {
        DateTimeOffset utc0000 = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)); //2000-1-1 00:00:00 +00:00
        DateTimeOffset hkt1200 = new DateTimeOffset(2000, 1, 1, 8 + 4, 0, 0, new TimeSpan(8, 0, 0)); //2000-1-1 12:00:00 +08:00
        DateTimeOffset dtu = hkt1200.ToUTCDateOnly(); //2000-1-1 00:00:00 +00:00 == f(2000-1-1 12:00:00 +08:00)
        Assert.Equal(utc0000, dtu);
    }

    [Fact]
    public void ToUTCDateOnly_NextDay1()
    {
        //2000-1-1 00:00:00 +00:00
        DateTimeOffset utc0000 = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)); 
        //2000-1-1 20:00:00 +08:00 -> 2000-1-1 12:00:00 +00:00
        DateTimeOffset hkt2000 = new DateTimeOffset(2000, 1, 1, 20, 0, 0, new TimeSpan(8, 0, 0)); 
        DateTimeOffset dtu = hkt2000.ToUTCDateOnly();
        Assert.Equal(utc0000, dtu);
    }

    [Fact]
    public void ToUTCDateOnly_NextDay2()
    {
        //2000-1-1 00:00:00 +00:00
        DateTimeOffset utc0000 = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)); 
        //2000-1-2 04:00:00 +08:00 -> 2000-1-1 20:00:00 +00:00
        DateTimeOffset hkt2000 = new DateTimeOffset(2000, 1, 2, 4, 0, 0, new TimeSpan(8, 0, 0)); 
        DateTimeOffset dtu = hkt2000.ToUTCDateOnly();
        Assert.Equal(utc0000, dtu);
    }

    [Fact]
    public void GetAsOffset() {
        DateTimeOffset utc2000 = new DateTimeOffset(2000, 1, 1, 20, 0, 0, new TimeSpan(0, 0, 0));
        DateTimeOffset hkt2000 = new DateTimeOffset(2000, 1, 1, 20, 0, 0, new TimeSpan(8, 0, 0));
        DateTimeOffset dtu = hkt2000.GetAsOffset(0);
        Assert.Equal(utc2000, dtu);
    }
}