using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class ScanImportController : Controller
{
    // Captura:
    // 1) itemId
    // 2) buyoutCopper
    // 3) stack
    // 4) timestamp (por ahora no lo usamos, pero nos sirve)
    private static readonly Regex RopeEntryRegex = new(
        @"\|Hitem:(\d+):[^|]*\|h\[.*?\]\|h\|r"",\s*\d+,\s*""[^""]+"",\s*""[^""]+"",\s*\d+,\s*(\d+),\s*(\d+),\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly AppDbContext _db;
    public ScanImportController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200_000_000)] // 200MB por las dudas
    public async Task<IActionResult> Index(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Msg"] = "ERROR: no subiste ningún archivo.";
            return RedirectToAction(nameof(Index));
        }

        var importedAtUtc = DateTime.UtcNow;

        var perItemPricesGold = new Dictionary<int, List<decimal>>();
        var perItemQty = new Dictionary<int, int>();

        int ropesSeen = 0, rowsParsed = 0, rowsFailed = 0;
        int linesWithHitem = 0;

        DateTime? scanTimeUtc = null;

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            // timestamp del scan (si aparece)
            if (scanTimeUtc is null)
            {
                var tm = Regex.Match(line, @"(ImageUpdated|LastFullScan)\s*=\s*(\d+)");
                if (tm.Success && long.TryParse(tm.Groups[2].Value, out var unix))
                {
                    scanTimeUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
                }
            }

            // solo líneas con Hitem
            if (!line.Contains("|Hitem:", StringComparison.OrdinalIgnoreCase))
                continue;

            linesWithHitem++;
            ropesSeen++;

            // En ScanData.lua hay strings con comillas escapadas \"...\" -> normalizamos a "..."
            var normalized = line.Replace("\\\"", "\"");

            var matches = RopeEntryRegex.Matches(normalized);
            if (matches.Count == 0)
                continue;

            foreach (Match m in matches)
            {
                if (!int.TryParse(m.Groups[1].Value, out var itemId)) { rowsFailed++; continue; }
                if (!long.TryParse(m.Groups[2].Value, out var buyoutCopper) || buyoutCopper <= 0) { rowsFailed++; continue; }
                if (!int.TryParse(m.Groups[3].Value, out var stack) || stack <= 0) stack = 1;

                // precio por unidad (copper -> gold)
                var perUnitGold = (buyoutCopper / (decimal)stack) / 10000m;

                if (!perItemPricesGold.TryGetValue(itemId, out var list))
                {
                    list = new List<decimal>(32);
                    perItemPricesGold[itemId] = list;
                }

                list.Add(perUnitGold);

                perItemQty[itemId] = (perItemQty.TryGetValue(itemId, out var q) ? q : 0) + stack;

                rowsParsed++;
            }
        }

        var ts = scanTimeUtc ?? importedAtUtc;

        // Upsert Items + insert PriceHistory snapshots
        var ids = perItemPricesGold.Keys.ToList();
        var urls = ids.Select(id => $"https://ah.nerfed.net/item/index?id={id}&faction=1&realm=17").ToList();

        var existingByUrl = await _db.Items
            .Where(x => urls.Contains(x.Url))
            .ToDictionaryAsync(x => x.Url, ct);

        var existingByItemId = await _db.Items
            .Where(x => x.WowItemId != null && ids.Contains(x.WowItemId.Value))
            .ToDictionaryAsync(x => x.WowItemId!.Value, ct);

        int itemsCreated = 0, itemsUpdated = 0, historyInserted = 0;

        foreach (var id in ids)
        {
            var prices = perItemPricesGold[id];
            if (prices.Count == 0) continue;

            prices.Sort();
            var median = MedianSorted(prices);
            var min = prices[0];
            var qtySum = perItemQty.TryGetValue(id, out var qq) ? qq : prices.Count;

            var url = $"https://ah.nerfed.net/item/index?id={id}&faction=1&realm=17";

            Item? item = null;

            // prioridad: match por WowItemId
            if (!existingByItemId.TryGetValue(id, out item))
            {
                // fallback: match por Url (para items viejos sin WowItemId)
                existingByUrl.TryGetValue(url, out item);
            }

            if (item is null)
            {
                item = new Item
                {
                    WowItemId = id,
                    Name = $"Item {id}",
                    Url = url,
                    Faction = 1,
                    Realm = 17
                };
                _db.Items.Add(item);
                itemsCreated++;
            }
            else
            {
                item.WowItemId ??= id;
                itemsUpdated++;
            }

            item.LastMedianBuyoutGold = median;
            item.LastMinBuyoutGold = min;
            item.LastQty = qtySum;
            item.LastFetchedAtUtc = ts;

            _db.PriceHistory.Add(new PriceHistory
            {
                Item = item,
                TimestampUtc = ts,
                MedianBuyoutGold = median,
                MinBuyoutGold = min,
                Qty = qtySum
            });
            historyInserted++;
        }

        await _db.SaveChangesAsync(ct);

        TempData["Msg"] =
            $"OK: linesWithHitem={linesWithHitem}, ropesSeen={ropesSeen}, parsed={rowsParsed}, failed={rowsFailed}, items+={itemsCreated}, items~={itemsUpdated}, history+={historyInserted}, ts={ts:u}";

        return RedirectToAction(nameof(Index));
    }

    private static decimal MedianSorted(List<decimal> sorted)
    {
        var n = sorted.Count;
        if (n == 0) return 0;
        var mid = n / 2;
        return (n % 2 == 1) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
    }
}
