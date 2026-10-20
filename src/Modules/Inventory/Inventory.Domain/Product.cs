using Erp.SharedKernel;

namespace Inventory.Domain;

public sealed class Product : AggregateRoot<Guid>
{
    private Product() { }

    public Product(Guid id, Guid tenantId, string sku, string name, decimal unitPrice, int stockQuantity)
        : base(id)
    {
        TenantId = tenantId;
        Sku = sku;
        Name = name;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    public Guid TenantId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public static Result<Product> Create(Guid tenantId, string sku, string name, decimal unitPrice, int initialStock)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<Product>(new Error("Product.TenantRequired", "Tenant ID is required."));

        if (string.IsNullOrWhiteSpace(sku))
            return Result.Failure<Product>(new Error("Product.SkuRequired", "SKU is required."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(new Error("Product.NameRequired", "Product name is required."));

        return new Product(Guid.NewGuid(), tenantId, sku.Trim().ToUpperInvariant(), name.Trim(), unitPrice, initialStock);
    }

    public void AdjustStock(int quantity)
    {
        StockQuantity += quantity;
    }
}
