using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain;

namespace Tenancy.Application.Features.Tenants.Queries;

public record TenantResponse(
    Guid TenantId,
    string? AppId,
    string? LegalId,
    string CountryCode,
    string Status,
    string ShardKey,
    string? CustomConnectionString);

public record GetTenantByIdQuery(Guid Id) : IQuery<TenantResponse>;
public record GetTenantByAppIdQuery(string AppId) : IQuery<TenantResponse>;
public record GetTenantByLegalIdQuery(string LegalId, string CountryCode) : IQuery<TenantResponse>;
public record GetAllTenantsQuery : IQuery<IReadOnlyList<TenantResponse>>;

public class TenantQueryHandlers(ITenancyDbContext dbContext)
    : IQueryHandler<GetTenantByIdQuery, TenantResponse>,
      IQueryHandler<GetTenantByAppIdQuery, TenantResponse>,
      IQueryHandler<GetTenantByLegalIdQuery, TenantResponse>,
      IQueryHandler<GetAllTenantsQuery, IReadOnlyList<TenantResponse>>
{
    private static TenantResponse Map(Tenant t) =>
        new(t.Id, t.AppId, t.LegalId, t.CountryCode, t.Status.ToString(), t.ShardKey, t.CustomConnectionString);

    public async Task<Result<TenantResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure<TenantResponse>(new Error("Tenant.NotFound", $"Tenant with ID {request.Id} was not found."));
        }

        return Result.Success(Map(tenant));
    }

    public async Task<Result<TenantResponse>> Handle(GetTenantByAppIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.AppId == request.AppId, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure<TenantResponse>(new Error("Tenant.NotFound", $"Tenant with AppId '{request.AppId}' was not found."));
        }

        return Result.Success(Map(tenant));
    }

    public async Task<Result<TenantResponse>> Handle(GetTenantByLegalIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.LegalId == request.LegalId && t.CountryCode == request.CountryCode, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure<TenantResponse>(new Error("Tenant.NotFound", $"Tenant with LegalId '{request.LegalId}' in country '{request.CountryCode}' was not found."));
        }

        return Result.Success(Map(tenant));
    }

    public async Task<Result<IReadOnlyList<TenantResponse>>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var responses = tenants.Select(Map).ToList() as IReadOnlyList<TenantResponse>;
        return Result.Success(responses);
    }
}
