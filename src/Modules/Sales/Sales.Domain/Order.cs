using Erp.SharedKernel;

namespace Sales.Domain;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}

public class OrderLine : Entity<Guid>
{
    private OrderLine() { }

    public OrderLine(Guid id, Guid productId, string productName, int quantity, decimal unitPrice)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = quantity * unitPrice;
    }

    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
}

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public Order(Guid id, Guid tenantId, string customerName, IEnumerable<OrderLine> lines)
        : base(id)
    {
        TenantId = tenantId;
        CustomerName = customerName;
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        _lines.AddRange(lines);
    }

    public Guid TenantId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalAmount => _lines.Sum(l => l.LineTotal);
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Result<Order> Create(Guid tenantId, string customerName, IEnumerable<OrderLine> lines)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<Order>(new Error("Order.TenantRequired", "Tenant ID is required."));

        if (string.IsNullOrWhiteSpace(customerName))
            return Result.Failure<Order>(new Error("Order.CustomerRequired", "Customer name is required."));

        var lineList = lines.ToList();
        if (lineList.Count == 0)
            return Result.Failure<Order>(new Error("Order.LinesRequired", "At least one line is required."));

        return new Order(Guid.NewGuid(), tenantId, customerName, lineList);
    }

    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedDomainEvent(Id, TenantId, TotalAmount));
    }
}

public record OrderConfirmedDomainEvent(Guid OrderId, Guid TenantId, decimal TotalAmount) : DomainEvent;
