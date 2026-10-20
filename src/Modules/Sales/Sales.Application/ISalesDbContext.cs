using Erp.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Application;

public interface ISalesDbContext : IUnitOfWork
{
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
}
