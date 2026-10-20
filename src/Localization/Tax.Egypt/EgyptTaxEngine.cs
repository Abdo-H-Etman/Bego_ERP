using Accounting.Application.Tax;

namespace Tax.Egypt;

public class EgyptTaxEngine : ITaxEngine
{
    public const decimal StandardVatRate = 0.14m; // 14% Egyptian VAT law

    public string CountryCode => "EG";

    public TaxResult Calculate(decimal subtotal)
    {
        var taxAmount = Math.Round(subtotal * StandardVatRate, 2);
        var total = subtotal + taxAmount;
        return new TaxResult(StandardVatRate, taxAmount, total, "Egypt Standard VAT (14%) - Egyptian Tax Authority ETA compliant");
    }
}
