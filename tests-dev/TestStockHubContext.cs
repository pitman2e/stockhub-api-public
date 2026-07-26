using System;
using Microsoft.Extensions.Configuration;
using StockHub.Database;

namespace UnitTestsDev;

public class TestStockHubContext
{
    public static StockHubContext Get()
    {
        var constr = Environment.GetEnvironmentVariable("DATABASE_CONSTR");

        if (string.IsNullOrWhiteSpace(constr))
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<TestStockHubContext>()
                .Build();
            constr = configuration["ConnectionString:StockHubDatabase"];
        }

        if (constr == null)
        {
            throw new ArgumentException("No connection string found");
        }

        return new StockHubContext(constr);
    }
}