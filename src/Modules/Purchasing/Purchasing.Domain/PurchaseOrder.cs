using Erp.SharedKernel;

namespace Purchasing.Domain;

public enum PurchaseOrderStatus
{
    Pending,
    Approved,
    Received,
    Cancelled
}

public class PurchaseOrderLine : Entity<Guid>
{
    private PurchaseOrderLine() { }

    public PurchaseOrderLine(Guid id, string itemName, int quantity, decimal unitCost)
        : base(id)
    {
        ItemName = itemName;
        Quantity = quantity;
        UnitCost = unitCost;
        LineTotal = quantity * unitCost;
    }

    public string ItemName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
}

public sealed class PurchaseOrder : AggregateRoot<Guid>
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder() { }

    public PurchaseOrder(Guid id, Guid tenantId, string vendorName, IEnumerable<PurchaseOrderLine> lines)
        : base(id)
    {
        TenantId = tenantId;
        VendorName = vendorName;
        Status = PurchaseOrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        _lines.AddRange(lines);
    }

    public Guid TenantId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalAmount => _lines.Sum(l => l.LineTotal);
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public static Result<PurchaseOrder> Create(Guid tenantId, string vendorName, IEnumerable<PurchaseOrderLine> lines)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<PurchaseOrder>(new Error("PurchaseOrder.TenantRequired", "Tenant ID is required."));

        if (string.IsNullOrWhiteSpace(vendorName))
            return Result.Failure<PurchaseOrder>(new Error("PurchaseOrder.VendorRequired", "Vendor name is required."));

        var lineList = lines.ToList();
        if (lineList.Count == 0)
            return Result.Failure<PurchaseOrder>(new Error("PurchaseOrder.LinesRequired", "At least one line is required."));

        return new PurchaseOrder(Guid.NewGuid(), tenantId, vendorName, lineList);
    }
}
