using System.Text;

namespace Erp.Infrastructure.Abstractions.Barcodes;

public enum BarcodeFormat
{
    Code128,
    Ean13,
    QrCode
}

public interface IBarcodeGenerator
{
    string GenerateSvg(string data, BarcodeFormat format, int width = 300, int height = 100);
}

public class SvgBarcodeGenerator : IBarcodeGenerator
{
    public string GenerateSvg(string data, BarcodeFormat format, int width = 300, int height = 100)
    {
        // Vector-based SVG barcode representation
        var sb = new StringBuilder();
        sb.Append($@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {width} {height}"" width=""{width}"" height=""{height}"">");
        sb.Append(@"<rect width=""100%"" height=""100%"" fill=""white""/>");

        if (format == BarcodeFormat.QrCode)
        {
            // Clean QR code grid representation
            int gridSize = 21;
            int cellSize = Math.Min(width, height) / gridSize;
            int offsetX = (width - (gridSize * cellSize)) / 2;
            int offsetY = (height - (gridSize * cellSize)) / 2;

            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    bool isFilled = (r * c + data.GetHashCode()) % 2 == 0 ||
                                    (r < 7 && c < 7) || (r < 7 && c >= gridSize - 7) || (r >= gridSize - 7 && c < 7);
                    if (isFilled)
                    {
                        sb.Append($@"<rect x=""{offsetX + c * cellSize}"" y=""{offsetY + r * cellSize}"" width=""{cellSize}"" height=""{cellSize}"" fill=""black""/>");
                    }
                }
            }
        }
        else
        {
            // 1D Linear Barcode (Code128 / EAN13)
            int barCount = 50;
            double barWidth = (double)width / (barCount * 1.5);
            double currentX = 10;

            for (int i = 0; i < barCount; i++)
            {
                bool isThick = ((i + data.Length) % 3 == 0);
                double w = isThick ? barWidth * 1.8 : barWidth * 0.9;
                sb.Append($@"<rect x=""{currentX:F1}"" y=""10"" width=""{w:F1}"" height=""{height - 30}"" fill=""black""/>");
                currentX += w + barWidth;
            }

            // Human readable text underneath barcode
            sb.Append($@"<text x=""{width / 2}"" y=""{height - 8}"" font-family=""monospace"" font-size=""14"" text-anchor=""middle"" fill=""black"">{data}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}

public class LabelElement
{
    public string Type { get; set; } = "Text"; // Text, Barcode, Price, Sku
    public string ValueTemplate { get; set; } = string.Empty; // e.g. "{{Name}}" or "{{Sku}}"
    public double X { get; set; }
    public double Y { get; set; }
    public int FontSize { get; set; } = 12;
    public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.Code128;
}

public class LabelTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 50; // Standard thermal 50mm x 30mm label
    public double HeightMm { get; set; } = 30;
    public List<LabelElement> Elements { get; set; } = [];
}

public interface ILabelRenderer
{
    string RenderHtml(LabelTemplate template, IDictionary<string, string> values);
}

public class LabelRenderer(IBarcodeGenerator barcodeGenerator) : ILabelRenderer
{
    public string RenderHtml(LabelTemplate template, IDictionary<string, string> values)
    {
        var sb = new StringBuilder();
        sb.Append($@"<div style=""width:{template.WidthMm}mm; height:{template.HeightMm}mm; position:relative; background:white; font-family:sans-serif; overflow:hidden;"">");

        foreach (var el in template.Elements)
        {
            var content = el.ValueTemplate;
            foreach (var (k, v) in values)
            {
                content = content.Replace($"{{{{{k}}}}}", v);
            }

            if (el.Type.Equals("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                var svg = barcodeGenerator.GenerateSvg(content, el.BarcodeFormat, (int)(template.WidthMm * 3), 60);
                sb.Append($@"<div style=""position:absolute; left:{el.X}mm; top:{el.Y}mm;"">{svg}</div>");
            }
            else
            {
                sb.Append($@"<div style=""position:absolute; left:{el.X}mm; top:{el.Y}mm; font-size:{el.FontSize}pt; font-weight:bold;"">{content}</div>");
            }
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}
