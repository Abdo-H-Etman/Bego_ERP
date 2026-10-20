using FluentAssertions;
using Tax.Egypt;
using Tax.Ksa;
using Tax.Uae;
using Xunit;

namespace Erp.UnitTests.Tax;

public class TaxEngineTests
{
    [Theory]
    [InlineData(100.0, 14.0, 114.0)]
    [InlineData(500.0, 70.0, 570.0)]
    public void EgyptTaxEngine_Should_Calculate14PercentVat(decimal subtotal, decimal expectedTax, decimal expectedTotal)
    {
        var engine = new EgyptTaxEngine();
        var result = engine.Calculate(subtotal);

        result.TaxRate.Should().Be(0.14m);
        result.TaxAmount.Should().Be(expectedTax);
        result.TotalWithTax.Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData(100.0, 15.0, 115.0)]
    [InlineData(200.0, 30.0, 230.0)]
    public void KsaTaxEngine_Should_Calculate15PercentVat(decimal subtotal, decimal expectedTax, decimal expectedTotal)
    {
        var engine = new KsaTaxEngine();
        var result = engine.Calculate(subtotal);

        result.TaxRate.Should().Be(0.15m);
        result.TaxAmount.Should().Be(expectedTax);
        result.TotalWithTax.Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData(100.0, 5.0, 105.0)]
    [InlineData(1000.0, 50.0, 1050.0)]
    public void UaeTaxEngine_Should_Calculate5PercentVat(decimal subtotal, decimal expectedTax, decimal expectedTotal)
    {
        var engine = new UaeTaxEngine();
        var result = engine.Calculate(subtotal);

        result.TaxRate.Should().Be(0.05m);
        result.TaxAmount.Should().Be(expectedTax);
        result.TotalWithTax.Should().Be(expectedTotal);
    }
}
