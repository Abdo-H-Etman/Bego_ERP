using Erp.Application.Abstractions.Messaging;
using Erp.Application.Abstractions.Tenancy;
using Erp.SharedKernel;
using Purchasing.Domain;

namespace Purchasing.Application.Features.PurchaseOrders.Commands;

public record PurchaseOrderLineDto(string ItemName, int Quantity, decimal UnitCost);

public record CreatePurchaseOrderCommand(string VendorName, List<PurchaseOrderLineDto> Lines) : ICommand<Guid>;

public class CreatePurchaseOrderCommandHandler(IPurchasingDbContext dbContext, ITenantContext tenantContext)
    : ICommandHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Lines.Select(l =>
            new PurchaseOrderLine(Guid.NewGuid(), l.ItemName, l.Quantity, l.UnitCost));

        var orderResult = PurchaseOrder.Create(tenantContext.TenantId, request.VendorName, lines);
        if (orderResult.IsFailure) return Result.Failure<Guid>(orderResult.Error);

        var order = orderResult.Value;
        await dbContext.PurchaseOrders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
