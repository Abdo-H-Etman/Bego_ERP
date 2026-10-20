using Accounting.Domain;
using Erp.Application.Abstractions.Messaging;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Features.Invoices.Queries;

public record InvoiceResponse(
    Guid Id,
    Guid TenantId,
    string InvoiceNumber,
    string CustomerName,
    string Currency,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    DateTime IssueDate);

public record GetInvoicesQuery : IQuery<IReadOnlyList<InvoiceResponse>>;
public record GetInvoiceByIdQuery(Guid Id) : IQuery<InvoiceResponse>;

public class InvoiceQueryHandlers(IAccountingDbContext dbContext)
    : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceResponse>>,
      IQueryHandler<GetInvoiceByIdQuery, InvoiceResponse>
{
    private static InvoiceResponse Map(Invoice inv) =>
        new(inv.Id, inv.TenantId, inv.InvoiceNumber, inv.CustomerName, inv.Currency,
            inv.SubTotal, inv.TaxAmount, inv.TotalAmount, inv.Status.ToString(), inv.IssueDate);

    public async Task<Result<IReadOnlyList<InvoiceResponse>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = invoices.Select(Map).ToList() as IReadOnlyList<InvoiceResponse>;
        return Result.Success(response);
    }

    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure<InvoiceResponse>(new Error("Invoice.NotFound", $"Invoice with ID {request.Id} not found."));
        }

        return Result.Success(Map(invoice));
    }
}
