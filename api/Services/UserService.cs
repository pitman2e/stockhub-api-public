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
        var now = DateTimeOffset.UtcNow;
        
        var affectedRow = await context.Users
            .Where(u => u.Uid == userClaims.GetUid())
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.LastBeat, now)
            );

        // If User does not exist, create it
        if (affectedRow == 0)
        {
            // Sanity check, but just in case 
            var user = await context.Users.FindAsync(userClaims.GetUid());
            if (user == null)
            {
                user = new StockUser()
                {
                    Uid = userClaims.GetUid(),
                    LastBeat = now
                };
                context.Users.Add(user);
            }
            await context.SaveChangesAsync();
        }
    }
}
