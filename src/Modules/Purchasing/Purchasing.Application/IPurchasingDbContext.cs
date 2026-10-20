using Erp.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Purchasing.Domain;

namespace Purchasing.Application;

public interface IPurchasingDbContext : IUnitOfWork
{
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
}
