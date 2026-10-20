using Accounting.Application.Tax;
using Accounting.Domain;
using Erp.Application.Abstractions.Messaging;
using Erp.Application.Abstractions.Tenancy;
using Erp.SharedKernel;

namespace Accounting.Application.Features.Invoices.Commands;

public record InvoiceLineDto(string Description, decimal Quantity, decimal UnitPrice);

public record CreateInvoiceCommand(
    string CustomerName,
    string Currency,
    List<InvoiceLineDto> Lines) : ICommand<Guid>;

public class CreateInvoiceCommandHandler(
    IAccountingDbContext dbContext,
    ITaxEngineFactory taxEngineFactory,
    ITenantContext tenantContext) : ICommandHandler<CreateInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Guid>(new Error("Tenant.Missing", "No active tenant context found for operation."));
        }

        var taxEngine = taxEngineFactory.GetEngine();

        var lines = new List<InvoiceLine>();
        decimal subtotal = 0;

        foreach (var l in request.Lines)
        {
            var lineSubtotal = l.Quantity * l.UnitPrice;
            subtotal += lineSubtotal;
            var taxCalc = taxEngine.Calculate(lineSubtotal);
            lines.Add(new InvoiceLine(Guid.NewGuid(), l.Description, l.Quantity, l.UnitPrice, taxCalc.TaxRate));
        }

        var overallTax = taxEngine.Calculate(subtotal);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var invoiceResult = Invoice.Create(
            tenantId,
            invoiceNumber,
            request.CustomerName,
            request.Currency,
            overallTax.TaxAmount,
            lines);

        if (invoiceResult.IsFailure)
        {
            return Result.Failure<Guid>(invoiceResult.Error);
        }

        var invoice = invoiceResult.Value;
        invoice.Issue();

        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.Id);
    }
}
