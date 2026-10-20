using Erp.Application.Abstractions.Data;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application;

public interface IInventoryDbContext : IUnitOfWork
{
    DbSet<Product> Products { get; }
}
