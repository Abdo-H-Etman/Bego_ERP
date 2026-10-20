using Erp.Application.Abstractions.Messaging;
using Erp.Application.Abstractions.Tenancy;
using Erp.SharedKernel;
using Inventory.Domain;

namespace Inventory.Application.Features.Products.Commands;

public record CreateProductCommand(string Sku, string Name, decimal UnitPrice, int InitialStock) : ICommand<Guid>;

public class CreateProductCommandHandler(IInventoryDbContext dbContext, ITenantContext tenantContext)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productResult = Product.Create(
            tenantContext.TenantId,
            request.Sku,
            request.Name,
            request.UnitPrice,
            request.InitialStock);

        if (productResult.IsFailure) return Result.Failure<Guid>(productResult.Error);

        var product = productResult.Value;
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(product.Id);
    }
}
