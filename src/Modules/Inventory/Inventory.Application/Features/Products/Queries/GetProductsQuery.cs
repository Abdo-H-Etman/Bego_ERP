using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Products.Queries;

public record GetProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
public record ProductDto(Guid Id, string Sku, string Name, decimal UnitPrice, int StockQuantity);

public class GetProductsQueryHandler(IInventoryDbContext dbContext)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var dtos = await dbContext.Products
            .AsNoTracking()
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProductDto>>(dtos);
    }
}
