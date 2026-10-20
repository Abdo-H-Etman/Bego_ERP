using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Purchasing.Application.Features.PurchaseOrders.Queries;

public record GetPurchaseOrdersQuery : IQuery<IReadOnlyList<PurchaseOrderDto>>;
public record PurchaseOrderDto(Guid Id, Guid TenantId, string VendorName, string Status, decimal TotalAmount, DateTime CreatedAtUtc);

public class GetPurchaseOrdersQueryHandler(IPurchasingDbContext dbContext)
    : IQueryHandler<GetPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderDto>>
{
    public async Task<Result<IReadOnlyList<PurchaseOrderDto>>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var dtos = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Select(o => new PurchaseOrderDto(o.Id, o.TenantId, o.VendorName, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PurchaseOrderDto>>(dtos);
    }
}
