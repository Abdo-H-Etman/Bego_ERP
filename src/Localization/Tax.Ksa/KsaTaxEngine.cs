using Accounting.Application.Tax;

namespace Tax.Ksa;

public class KsaTaxEngine : ITaxEngine
{
    public const decimal StandardVatRate = 0.15m; // 15% Saudi Arabia ZATCA standard VAT

    public string CountryCode => "SA";

    public TaxResult Calculate(decimal subtotal)
    {
        var taxAmount = Math.Round(subtotal * StandardVatRate, 2);
        var total = subtotal + taxAmount;
        return new TaxResult(StandardVatRate, taxAmount, total, "KSA Standard VAT (15%) - ZATCA Fatoora Phase compliant");
    }
}
