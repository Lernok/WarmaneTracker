// ScanImportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class ScanImportController : Controller
{
    private static readonly Regex RopeEntryRegex = new(
     // 1) itemId
     // 2) entry (todo el bloque hasta false)
     @"(\{""\|c[0-9a-fA-F]{8}\|Hitem:(\d+):[^|]*\|h\[[^]]*\]\|h\|r"".*?,false)",
     RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking,
     TimeSpan.FromSeconds(10));

    private readonly AppDbContext _db;
    private readonly ILogger<ScanImportController> _logger;

    public ScanImportController(AppDbContext db, ILogger<ScanImportController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Index(IFormFile file, CancellationToken ct)
    {
        _logger.LogInformation("IMPORT POST hit. fileNull={null} len={len}", file is null, file?.Length ?? -1);
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

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Reading file into memory...");
        var content = await reader.ReadToEndAsync();
        _logger.LogInformation("ReadToEnd OK. chars={chars}", content.Length);

        var tm = Regex.Match(content, @"(ImageUpdated|LastFullScan)\s*=\s*(\d+)");
        if (tm.Success && long.TryParse(tm.Groups[2].Value, out var unix))
            scanTimeUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

        // Normaliza \" -> "
        var normalized = content
    .Replace("\\\"", "\"")
    .Replace("\\{", "{")
    .Replace("\\}", "}");

        _logger.LogInformation("Running regex... normalizedChars={chars}", normalized.Length);
        var matches = RopeEntryRegex.Matches(normalized);
        _logger.LogInformation("Regex done. matches={matches}", matches.Count);
        if (matches.Count > 0)
        {
            var m0 = matches[0];
            var start = Math.Max(0, m0.Index);
            var len = Math.Min(2000, normalized.Length - start);
            var chunk = normalized.Substring(start, len);
            _logger.LogInformation("FIRST ENTRY CHUNK:\n{chunk}", chunk);
        }

        _logger.LogInformation("Import scan: bytes={bytes} matches={matches}", file.Length, matches.Count);

        foreach (Match m in matches)
        {
            // entry completa hasta false
            var entry = m.Groups[1].Value;

            // itemId (ojo: ahora es group 2)
            if (!int.TryParse(m.Groups[2].Value, out var itemId)) { rowsFailed++; continue; }

            // Split por coma (CSV simple para los campos numéricos que necesitamos)
            var parts = entry.Split(',');

            // stack está en índice 6 según tu chunk:
            // 0={"|c..|Hitem..", 1=226, 2="Armor", 3="Cloth", 4=8, 5=startBid, 6=stack, 7=ts, ...
            int stack = 1;
            if (parts.Length > 6 && int.TryParse(parts[6], out var st) && st > 0)
                stack = st;

            // buyout: en tu chunk es el 3er elemento desde el final:
            // ..., minBid, bidInc, buyout, currentBid, false
            // buyout/minBid robusto (si no hay buyout, usamos minBid)
            long buyoutCopper = 0;

            int falseIndex = -1;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                var t = parts[i].Trim();
                if (t == "false" || t.StartsWith("false", StringComparison.Ordinal))
                {
                    falseIndex = i;
                    break;
                }
            }

            if (falseIndex < 0) { rowsFailed++; continue; }

            // normal: ... minBid, bidInc, buyout, currentBid, false
            long minBidCopper = 0;

            // buyout
            if (falseIndex >= 3 && long.TryParse(parts[falseIndex - 2].Trim(), out var bo) && bo > 0)
                buyoutCopper = bo;

            // minBid fallback
            if (falseIndex >= 5 && long.TryParse(parts[falseIndex - 4].Trim(), out var mb) && mb > 0)
                minBidCopper = mb;

            // si no hay buyout, usamos minBid
            if (buyoutCopper <= 0)
                buyoutCopper = minBidCopper;

            if (buyoutCopper <= 0) { rowsFailed++; continue; }



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


        // Para mantener tu mensaje final coherente
        linesWithHitem = normalized.Contains("|Hitem:", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        ropesSeen = linesWithHitem;


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

            if (!existingByItemId.TryGetValue(id, out item))
                existingByUrl.TryGetValue(url, out item);

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
        _logger.LogInformation("Import finished: ropesSeen={ropes} parsed={parsed} failed={failed} elapsedMs={ms}",
    ropesSeen, rowsParsed, rowsFailed, sw.ElapsedMilliseconds);


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
