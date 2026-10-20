using Erp.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain;

namespace Tenancy.Application;

public interface ITenancyDbContext : IUnitOfWork
{
    DbSet<Tenant> Tenants { get; }
}
