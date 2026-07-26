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
        var query = context.StockPortfolios
            .Include(p => p.FkStockVirtualPortfolios)
            .ByUid(userClaims.GetUid());
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

        var childPortfolioIds = await ValidateChildPortfolioIdsAsync(dto.PortfolioId, dto.IsVirtual, dto.ChildPortfolioIds);
        
        var portfolio = new StockPortfolio
        {
            Uid = userClaims.GetUid(),
            PortfolioId = dto.PortfolioId,
            Name = dto.PortfolioName,
            DefaultCurrency = dto.DefaultCurrency,
            Priority = dto.Priority,
            IsVirtual = dto.IsVirtual,
            FkStockVirtualPortfolios = []
        };
        context.Add(portfolio);
        AddChildPortfolioMappings(portfolio, childPortfolioIds);
        await context.SaveChangesAsync();
    }
    
    public async Task<StockPortfolio> UpdateAsync(PortfolioPutDto dto)
    {
        var portfolio = (await GetAsync([p => p.PortfolioId == dto.PortfolioId])).FirstOrDefault();
        if (portfolio == null)
        {
            throw new SHArgumentException($"Portfolio Id {dto.PortfolioId} does not exist");
        }

        var childPortfolioIds = await ValidateChildPortfolioIdsAsync(dto.PortfolioId, dto.IsVirtual, dto.ChildPortfolioIds);
        context.Entry(portfolio).Property(s => s.Version).OriginalValue = dto.Version;
        portfolio.Name = dto.PortfolioName;
        portfolio.DefaultCurrency = dto.DefaultCurrency;
        portfolio.Priority = dto.Priority;
        portfolio.IsVirtual = dto.IsVirtual;
        ReplaceChildPortfolioMappings(portfolio, childPortfolioIds);
        await context.SaveChangesAsync();
        return portfolio;
    }

    private async Task<List<string>> ValidateChildPortfolioIdsAsync(
        string portfolioId,
        bool isVirtual,
        IEnumerable<string>? proposedChildPortfolioIds)
    {
        var childPortfolioIds = proposedChildPortfolioIds?.ToList() ?? [];
        if (!isVirtual && childPortfolioIds.Count > 0)
        {
            throw new SHArgumentException("A real portfolio cannot have child portfolios");
        }

        if (childPortfolioIds.Count != childPortfolioIds.Distinct().Count())
        {
            throw new SHArgumentException("Child portfolio IDs must be unique");
        }

        var children = await context.StockPortfolios
            .Where(p => p.Uid == userClaims.GetUid())
            .Where(p => childPortfolioIds.Contains(p.PortfolioId))
            .ToListAsync();

        if (children.Count != childPortfolioIds.Count)
        {
            throw new SHArgumentException("One or more child portfolios do not exist");
        }

        if (children.Any(p => p.IsVirtual || p.PortfolioId == portfolioId))
        {
            throw new SHArgumentException("A virtual portfolio can only contain real portfolios");
        }

        return childPortfolioIds;
    }

    private void AddChildPortfolioMappings(StockPortfolio portfolio, IEnumerable<string> childPortfolioIds)
    {
        foreach (var childPortfolioId in childPortfolioIds)
        {
            portfolio.FkStockVirtualPortfolios.Add(new StockVirtualPortfolio
            {
                Uid = portfolio.Uid,
                PortfolioId = portfolio.PortfolioId,
                ChildPortfolioId = childPortfolioId
            });
        }
    }

    private void ReplaceChildPortfolioMappings(StockPortfolio portfolio, IEnumerable<string> childPortfolioIds)
    {
        var desiredChildPortfolioIds = childPortfolioIds.ToHashSet();
        var obsoleteMappings = portfolio.FkStockVirtualPortfolios
            .Where(mapping => !desiredChildPortfolioIds.Contains(mapping.ChildPortfolioId))
            .ToList();
        context.StockVirtualPortfolios.RemoveRange(obsoleteMappings);
        foreach (var obsoleteMapping in obsoleteMappings)
        {
            portfolio.FkStockVirtualPortfolios.Remove(obsoleteMapping);
        }

        var existingChildPortfolioIds = portfolio.FkStockVirtualPortfolios
            .Select(mapping => mapping.ChildPortfolioId)
            .ToHashSet();
        AddChildPortfolioMappings(
            portfolio,
            desiredChildPortfolioIds.Except(existingChildPortfolioIds));
    }
    
    public async Task<StockPortfolio> DeleteAsync(string portfolioId)
    {
        var portfolio = (await GetAsync([p => p.PortfolioId == portfolioId])).FirstOrDefault();
        if (portfolio == null)
        {
            throw new SHArgumentException($"Portfolio Id {portfolioId} does not exist");
        }
        context.Remove(portfolio);
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