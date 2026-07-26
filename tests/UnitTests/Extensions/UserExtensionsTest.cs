using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using StockHub.Database;
using StockHub.Extensions;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Extensions;

[TestSubject(typeof(UserExtensions))]
public class UserExtensionsTest
{
    [Fact]
    public void ByActive_ReturnsOnlyUsersWithBeatWithinLastFiveMinutes()
    {
        // Arrange
        var uid = UserClaimMock.Get().GetUid();
        var now = DateTimeOffset.UtcNow;
        var activeUserRecent = new StockUser { LastBeat = now, Uid = uid };
        var activeUserWithinBoundary = new StockUser { LastBeat = now.AddMinutes(-4.5), Uid = uid };
        var inactiveUserExpired = new StockUser { LastBeat = now.AddMinutes(-6), Uid = uid };
        var inactiveUserNull = new StockUser { LastBeat = null, Uid = uid };

        var usersQuery = new List<StockUser>
        {
            activeUserRecent,
            activeUserWithinBoundary,
            inactiveUserExpired,
            inactiveUserNull
        }.AsQueryable();

        // Act
        var result = usersQuery.ByActive().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(activeUserRecent, result);
        Assert.Contains(activeUserWithinBoundary, result);
        Assert.DoesNotContain(inactiveUserExpired, result);
        Assert.DoesNotContain(inactiveUserNull, result);
    }
}