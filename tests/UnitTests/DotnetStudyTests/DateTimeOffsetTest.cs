using System;
using Xunit;

namespace UnitTests.DotnetStudyTests;

public class DateTimeOffsetTest
{
    [Fact]
    public void BehaviourTest()
    {
        DateTimeOffset dtf1 = new DateTimeOffset(2000, 1, 1, 8, 0, 0, new TimeSpan(8, 0, 0));
        DateTimeOffset dtf2 = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0));
        Assert.Equal(dtf1, dtf2);
        Assert.Equal(dtf1.ToUniversalTime(), dtf2.ToUniversalTime());
    }

    [Fact]
    public void DateTimeOnly()
    {
        DateTimeOffset dtf1 = new DateTimeOffset(2000, 1, 1, 2, 0, 0, new TimeSpan(0, 0, 0)); //{1/1/2000 2:00:00 am +00:00}
        DateTimeOffset dtf2 = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(0, 0, 0)); //{1/1/2000 12:00:00 am +00:00}
        DateTimeOffset du = dtf1.Date; //DateTime converts to DateTimeOffset implicitly //{1/1/2000 12:00:00 am +08:00}
        DateTimeOffset du3 = dtf1.UtcDateTime; //DateTime converts to DateTimeOffset implicitly //{1/1/2000 12:00:00 am +08:00}
        DateTimeOffset du2 = dtf1.UtcDateTime.Date; //DateTime converts to DateTimeOffset implicitly {1/1/2000 12:00:00 am +00:00}
        Assert.Equal(du2, dtf2);
    }
}