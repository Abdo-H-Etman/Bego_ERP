using Erp.Infrastructure.Abstractions.Barcodes;
using FluentAssertions;
using Xunit;

namespace Erp.UnitTests.Barcodes;

public class BarcodeGeneratorTests
{
    [Fact]
    public void BarcodeGenerator_Should_GenerateSvg_ForCode128()
    {
        var generator = new SvgBarcodeGenerator();
        var svg = generator.GenerateSvg("PRD-100234", BarcodeFormat.Code128, 300, 100);

        svg.Should().StartWith("<svg");
        svg.Should().Contain("<rect");
        svg.Should().Contain("PRD-100234");
        svg.Should().EndWith("</svg>");
    }

    [Fact]
    public void BarcodeGenerator_Should_GenerateSvg_ForQrCode()
    {
        var generator = new SvgBarcodeGenerator();
        var svg = generator.GenerateSvg("https://erp.com/verify/inv-101", BarcodeFormat.QrCode, 200, 200);

        svg.Should().StartWith("<svg");
        svg.Should().Contain("<rect");
        svg.Should().EndWith("</svg>");
    }
}
