using Accounting.Domain;
using Erp.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application;

public interface IAccountingDbContext : IUnitOfWork
{
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
}
