using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Controllers.Portfolio;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Repositories;

public class PortfolioRepo(
    StockHubContext context,
    IUserClaims userClaims
    )
{
    public async Task<IEnumerable<StockPortfolio>> GetAsync(IEnumerable<Expression<Func<StockPortfolio, bool>>>? predicates)
    {
        var query = context.StockPortfolios.ByUid(userClaims.GetUid());
        foreach (var predicate in predicates ?? []) query = query.Where(predicate);
        var result = await query
            .OrderBy(p => p.Priority)
            .ToListAsync();

        return result;
    }

    public async Task InsertAsync(PortfolioPostDto dto)
    {
        var dbPortfolio = (await GetAsync([p => p.PortfolioId == dto.PortfolioId])).FirstOrDefault();
        if (dbPortfolio != null)
        {
            throw new SHArgumentException($"Portfolio Id {dto.PortfolioId} already exist");
        }
        
        var portfolio = new StockPortfolio
        {
            Uid = userClaims.GetUid(),
            PortfolioId = dto.PortfolioId,
            Name = dto.PortfolioName,
            DefaultCurrency = dto.DefaultCurrency,
            IsVirtual = dto.IsVirtual
        };
        context.Add(portfolio);
        await context.SaveChangesAsync();
    }
    
    public async Task<StockPortfolio> UpdateAsync(PortfolioPutDto dto)
    {
        var portfolio = (await GetAsync([p => p.PortfolioId == dto.PortfolioId])).FirstOrDefault();
        if (portfolio == null)
        {
            throw new SHArgumentException($"Portfolio Id {dto.PortfolioId} does not exist");
        }
        context.Entry(portfolio).Property(s => s.Version).OriginalValue = dto.Version;
        portfolio.Name = dto.PortfolioName;
        portfolio.DefaultCurrency = dto.DefaultCurrency;
        portfolio.IsVirtual = dto.IsVirtual;
        await context.SaveChangesAsync();
        return portfolio;
    }
    
    public async Task<StockPortfolio?> GetAsync(string portfolioId)
    {
        if (string.IsNullOrWhiteSpace(portfolioId))
        {
            return null;
        }

        var portfolio = await context.StockPortfolios
            .Where(p => p.Uid == userClaims.GetUid())
            .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);

        if (portfolio == null)
        {
            throw new SHArgumentException($"Portfolio Id ${portfolioId} does not exist");
        }

        return portfolio;
    }
}