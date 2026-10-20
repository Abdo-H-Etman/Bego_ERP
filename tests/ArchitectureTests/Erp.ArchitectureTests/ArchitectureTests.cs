using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Erp.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly string[] OtherModulesForAccounting =
        ["Inventory", "Sales", "Purchasing"];

    private static readonly string[] OtherModulesForInventory =
        ["Accounting", "Sales", "Purchasing"];

    private static readonly string[] OtherModulesForSales =
        ["Accounting", "Inventory", "Purchasing"];

    private static readonly string[] OtherModulesForPurchasing =
        ["Accounting", "Inventory", "Sales"];

    [Fact]
    public void Domain_Should_Not_DependOn_Application_Or_Infrastructure()
    {
        var domainAssembly = typeof(Accounting.Domain.Invoice).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Accounting.Application", "Accounting.Infrastructure", "Accounting.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain layer must not depend on Application, Infrastructure, or Api");
    }

    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure_Or_Api()
    {
        var appAssembly = typeof(Accounting.Application.Features.Invoices.Commands.CreateInvoiceCommand).Assembly;

        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Accounting.Infrastructure", "Accounting.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application layer must not depend on Infrastructure or Api");
    }

    [Fact]
    public void AccountingModule_Should_Not_DependOn_Other_Modules_Domain_Or_Infrastructure()
    {
        var accountingAssembly = typeof(Accounting.Application.Features.Invoices.Commands.CreateInvoiceCommand).Assembly;

        foreach (var other in OtherModulesForAccounting)
        {
            var result = Types.InAssembly(accountingAssembly)
                .ShouldNot()
                .HaveDependencyOnAny($"{other}.Domain", $"{other}.Infrastructure")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Accounting must not reference {other} Domain or Infrastructure");
        }
    }

    [Fact]
    public void InventoryModule_Should_Not_DependOn_Other_Modules_Domain_Or_Infrastructure()
    {
        var inventoryAssembly = typeof(Inventory.Application.Features.Products.Commands.CreateProductCommand).Assembly;

        foreach (var other in OtherModulesForInventory)
        {
            var result = Types.InAssembly(inventoryAssembly)
                .ShouldNot()
                .HaveDependencyOnAny($"{other}.Domain", $"{other}.Infrastructure")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Inventory must not reference {other} Domain or Infrastructure");
        }
    }

    [Fact]
    public void SalesModule_Should_Not_DependOn_Other_Modules_Domain_Or_Infrastructure()
    {
        var salesAssembly = typeof(Sales.Application.Features.Orders.Commands.CreateOrderCommand).Assembly;

        foreach (var other in OtherModulesForSales)
        {
            var result = Types.InAssembly(salesAssembly)
                .ShouldNot()
                .HaveDependencyOnAny($"{other}.Domain", $"{other}.Infrastructure")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Sales must not reference {other} Domain or Infrastructure");
        }
    }

    [Fact]
    public void PurchasingModule_Should_Not_DependOn_Other_Modules_Domain_Or_Infrastructure()
    {
        var purchasingAssembly = typeof(Purchasing.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrderCommand).Assembly;

        foreach (var other in OtherModulesForPurchasing)
        {
            var result = Types.InAssembly(purchasingAssembly)
                .ShouldNot()
                .HaveDependencyOnAny($"{other}.Domain", $"{other}.Infrastructure")
                .GetResult();

            result.IsSuccessful.Should().BeTrue($"Purchasing must not reference {other} Domain or Infrastructure");
        }
    }

    [Fact]
    public void TenancyDomain_Should_Not_DependOn_Any_Other_Module()
    {
        var tenancyDomainAssembly = typeof(Tenancy.Domain.Tenant).Assembly;

        var result = Types.InAssembly(tenancyDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Accounting", "Inventory", "Sales", "Purchasing")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Tenancy domain should have no knowledge of any business modules");
    }
}
