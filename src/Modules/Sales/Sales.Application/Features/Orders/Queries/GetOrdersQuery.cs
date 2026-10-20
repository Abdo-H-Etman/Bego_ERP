using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Sales.Application.Features.Orders.Queries;

public record GetOrdersQuery : IQuery<IReadOnlyList<OrderDto>>;
public record OrderDto(Guid Id, Guid TenantId, string CustomerName, string Status, decimal TotalAmount, DateTime CreatedAtUtc);

public class GetOrdersQueryHandler(ISalesDbContext dbContext)
    : IQueryHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var dtos = await dbContext.Orders
            .AsNoTracking()
            .Select(o => new OrderDto(o.Id, o.TenantId, o.CustomerName, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<OrderDto>>(dtos);
    }
}
