using System;
using StockHub.Database;

namespace UnitTests;

public static class TestStockHubContext
{
    public static StockHubContext? Get()
    {
        //Sample: "User ID=db_user_name;Password=db_user_password;Host=192.168.1.123;Port=5432;Database=sh_test;";
        var constr = Environment.GetEnvironmentVariable("DATABASE_CONSTR");

        if (string.IsNullOrWhiteSpace(constr))
        {
            return null;
        }

        return new StockHubContext(constr);
    }
}