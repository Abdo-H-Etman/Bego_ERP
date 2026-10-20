using Accounting.Application.Tax;

namespace Tax.Uae;

public class UaeTaxEngine : ITaxEngine
{
    public const decimal StandardVatRate = 0.05m; // 5% UAE Federal Tax Authority VAT

    public string CountryCode => "AE";

    public TaxResult Calculate(decimal subtotal)
    {
        var taxAmount = Math.Round(subtotal * StandardVatRate, 2);
        var total = subtotal + taxAmount;
        return new TaxResult(StandardVatRate, taxAmount, total, "UAE Standard VAT (5%) - FTA Federal Tax Authority compliant");
    }
}
