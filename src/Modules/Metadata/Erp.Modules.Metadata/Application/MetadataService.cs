using Erp.Infrastructure.Abstractions.Caching;
using Erp.Modules.Metadata.Domain;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Metadata.Application;

public record PageMetadataResponse(
    PageDefinition Page,
    FormDefinition? Form,
    GridDefinition? Grid,
    AuthorizationGridDefinition? GridAuthorization);

public interface IMetadataService
{
    Task<PageMetadataResponse?> GetPageMetadataAsync(string pageCode, string? userRole = null, CancellationToken cancellationToken = default);
    Task InvalidatePageCacheAsync(string pageCode, CancellationToken cancellationToken = default);
}

public class MetadataService(
    Infrastructure.MetadataDbContext dbContext,
    IRedisCacheService cacheService) : IMetadataService
{
    public async Task<PageMetadataResponse?> GetPageMetadataAsync(
        string pageCode,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = pageCode.Trim().ToLowerInvariant();
        var cacheKey = $"metadata:page:{normalizedCode}:{userRole ?? "anon"}";

        var cached = await cacheService.GetAsync<PageMetadataResponse>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var page = await dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);

        if (page is null) return null;

        var form = await dbContext.Forms
            .Include(f => f.Fields)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.PageCode == normalizedCode, cancellationToken);

        var grid = await dbContext.Grids
            .Include(g => g.Columns)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.PageCode == normalizedCode, cancellationToken);

        AuthorizationGridDefinition? authGrid = null;
        if (grid is not null && !string.IsNullOrWhiteSpace(userRole))
        {
            authGrid = await dbContext.AuthorizationGrids
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.GridCode == grid.GridCode && a.RoleOrPermissionCode == userRole, cancellationToken);
        }

        var response = new PageMetadataResponse(page, form, grid, authGrid);
        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1), cancellationToken);

        return response;
    }

    public async Task InvalidatePageCacheAsync(string pageCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = pageCode.Trim().ToLowerInvariant();
        await cacheService.RemoveByPrefixAsync($"metadata:page:{normalizedCode}", cancellationToken);
    }
}
