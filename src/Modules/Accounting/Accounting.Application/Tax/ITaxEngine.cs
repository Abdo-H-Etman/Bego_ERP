namespace Accounting.Application.Tax;

public record TaxResult(decimal TaxRate, decimal TaxAmount, decimal TotalWithTax, string TaxDetails);

public interface ITaxEngine
{
    string CountryCode { get; }
    TaxResult Calculate(decimal subtotal);
}

public interface ITaxEngineFactory
{
    ITaxEngine GetEngine();
}
