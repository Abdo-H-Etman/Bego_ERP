using Erp.Application.Abstractions.Messaging;
using Erp.Application.Abstractions.Tenancy;
using Erp.SharedKernel;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

public record OrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(string CustomerName, List<OrderLineDto> Lines) : ICommand<Guid>;

public class CreateOrderCommandHandler(ISalesDbContext dbContext, ITenantContext tenantContext)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Lines.Select(l =>
            new OrderLine(Guid.NewGuid(), l.ProductId, l.ProductName, l.Quantity, l.UnitPrice));

        var orderResult = Order.Create(tenantContext.TenantId, request.CustomerName, lines);
        if (orderResult.IsFailure) return Result.Failure<Guid>(orderResult.Error);

        var order = orderResult.Value;
        order.Confirm();

        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
