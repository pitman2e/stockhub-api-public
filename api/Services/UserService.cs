using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Interfaces;

namespace StockHub.Services;

public class UserService(
    StockHubContext context, 
    IUserClaims userClaims)
{
    public async Task PingAsync()
    {
        await context.Users
            .Where(u => u.Uid == userClaims.GetUid())
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.LastBeat, DateTimeOffset.Now.ToUniversalTime())
            );
    }
}
