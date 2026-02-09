using System.Globalization;
using System.Text.RegularExpressions;

namespace WarmaneTracker.Web.Services;

public record AhSnapshot(decimal Median, decimal Min, int Qty);

public class AhScraper
{
    private readonly IHttpClientFactory _httpFactory;

    public AhScraper(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public async Task<AhSnapshot> FetchAsync(string url, CancellationToken ct = default)
    {
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (WarmaneTracker; +local)");

        var html = await http.GetStringAsync(url, ct);

        // Intento 1: buscar labels típicos (robusto a pequeños cambios)
        // Nota: si falla, ajustamos regex mirando un fragmento real del HTML.
        var median = ExtractDecimal(html, @"Median\s*Buyout[^0-9]*([0-9]+(?:\.[0-9]+)?)");
        var min = ExtractDecimal(html, @"Min\s*Buyout[^0-9]*([0-9]+(?:\.[0-9]+)?)");
        var qty = ExtractInt(html, @"Quantity[^0-9]*([0-9]+)");

        return new AhSnapshot(median, min, qty);
    }

    private static decimal ExtractDecimal(string html, string pattern)
    {
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!m.Success) throw new Exception($"No pude extraer decimal. Pattern: {pattern}");
        return decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static int ExtractInt(string html, string pattern)
    {
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!m.Success) throw new Exception($"No pude extraer int. Pattern: {pattern}");
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
