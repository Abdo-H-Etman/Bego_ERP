using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Tenancy.Domain;

namespace Tenancy.Application.Features.Tenants.Commands;

public record RegisterTenantCommand(
    string? AppId,
    string? LegalId,
    string CountryCode,
    string ShardKey = "shared-s1",
    string? CustomConnectionString = null) : ICommand<Guid>;

public class RegisterTenantCommandHandler(ITenancyDbContext dbContext)
    : ICommandHandler<RegisterTenantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var tenantResult = Tenant.Create(
            request.AppId,
            request.LegalId,
            request.CountryCode,
            request.ShardKey,
            request.CustomConnectionString);

        if (tenantResult.IsFailure)
        {
            return Result.Failure<Guid>(tenantResult.Error);
        }

        var tenant = tenantResult.Value;
        await dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(tenant.Id);
    }
}
