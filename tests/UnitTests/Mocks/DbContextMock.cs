using System;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;

namespace UnitTests.Mocks;

public abstract class DbContextMock
{
    public static StockHubContext Get()
    {
        // Return a new database because of the unique Guid
        var options = new DbContextOptionsBuilder<StockHubContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StockHubContext(options);
    }
}