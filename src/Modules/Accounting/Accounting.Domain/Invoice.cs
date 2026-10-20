using Erp.SharedKernel;

namespace Accounting.Domain;

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    Cancelled
}

public class InvoiceLine : Entity<Guid>
{
    private InvoiceLine() { }

    public InvoiceLine(Guid id, string description, decimal quantity, decimal unitPrice, decimal taxRate)
        : base(id)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        LineTotal = quantity * unitPrice;
    }

    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal LineTotal { get; private set; }
}

public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice() { } // EF Core

    private Invoice(
        Guid id,
        Guid tenantId,
        string invoiceNumber,
        string customerName,
        string currency,
        decimal taxAmount,
        DateTime issueDate)
        : base(id)
    {
        TenantId = tenantId;
        InvoiceNumber = invoiceNumber;
        CustomerName = customerName;
        Currency = currency;
        TaxAmount = taxAmount;
        IssueDate = issueDate;
        Status = InvoiceStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "EGP";
    public decimal SubTotal => _lines.Sum(l => l.LineTotal);
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount => SubTotal + TaxAmount;
    public DateTime IssueDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public static Result<Invoice> Create(
        Guid tenantId,
        string invoiceNumber,
        string customerName,
        string currency,
        decimal taxAmount,
        IEnumerable<InvoiceLine> lines)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Invoice>(new Error("Invoice.TenantRequired", "Tenant ID is required for invoice creation."));
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return Result.Failure<Invoice>(new Error("Invoice.CustomerRequired", "Customer name is required."));
        }

        var lineList = lines.ToList();
        if (lineList.Count == 0)
        {
            return Result.Failure<Invoice>(new Error("Invoice.EmptyLines", "Invoice must contain at least one line."));
        }

        var invoice = new Invoice(
            Guid.NewGuid(),
            tenantId,
            invoiceNumber,
            customerName,
            currency,
            taxAmount,
            DateTime.UtcNow);

        invoice._lines.AddRange(lineList);
        return invoice;
    }

    public void Issue()
    {
        Status = InvoiceStatus.Issued;
        AddDomainEvent(new InvoiceIssuedDomainEvent(Id, TenantId, TotalAmount, Currency));
    }
}

public record InvoiceIssuedDomainEvent(Guid InvoiceId, Guid TenantId, decimal TotalAmount, string Currency) : DomainEvent;
