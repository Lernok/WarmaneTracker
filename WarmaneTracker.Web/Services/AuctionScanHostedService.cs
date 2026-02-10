// AuctionScanHostedService.cs
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Services;

public sealed class AuctionScanHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuctionScanHostedService> _logger;

    // Onyxia id=17, faction=1
    private const string ScanUrl = "https://ah.nerfed.net/realm/getfile?id=17&faction=1";

    // Groups:
    // 1) full entry (up to "false")
    // 2) itemId
    private static readonly Regex RopeEntryRegex = new(
        @"(\{""\|c[0-9a-fA-F]{8}\|Hitem:(\d+):[^|]*\|h\[[^]]*\]\|h\|r"".*?,false)",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking,
        TimeSpan.FromSeconds(10));

    public AuctionScanHostedService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<AuctionScanHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // loop cada 1 hora
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auction scan job failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient();
        http.Timeout = TimeSpan.FromMinutes(5);

        _logger.LogInformation("Downloading scan: {url}", ScanUrl);

        var bytes = await http.GetByteArrayAsync(ScanUrl, ct);
        var text = Encoding.UTF8.GetString(bytes);

        // Parse
        var perItemPricesGold = new Dictionary<int, List<decimal>>();
        var perItemQty = new Dictionary<int, int>();

        int rowsParsed = 0, rowsFailed = 0;
        DateTime? scanTimeUtc = null;

        // timestamp
        var tm = Regex.Match(text, @"(ImageUpdated|LastFullScan)\s*=\s*(\d+)");
        if (tm.Success && long.TryParse(tm.Groups[2].Value, out var unix))
            scanTimeUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

        // normalize escapes
        var normalized = text
            .Replace("\\\"", "\"")
            .Replace("\\{", "{")
            .Replace("\\}", "}");

        var matches = RopeEntryRegex.Matches(normalized);
        _logger.LogInformation("Hosted scan regex matches={matches}", matches.Count);

        foreach (Match m in matches)
        {
            var entry = m.Groups[1].Value;

            if (!int.TryParse(m.Groups[2].Value, out var itemId))
            {
                rowsFailed++;
                continue;
            }

            var parts = entry.Split(',');

            // stack index (as validated in your FIRST ENTRY CHUNK)
            int stack = 1;
            if (parts.Length > 6 && int.TryParse(parts[6], out var st) && st > 0)
                stack = st;

            // buyout fallback to minBid if buyout=0
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
            if (falseIndex < 0)
            {
                rowsFailed++;
                continue;
            }

            long minBidCopper = 0;

            // ... minBid, bidInc, buyout, currentBid, false
            if (falseIndex >= 3 && long.TryParse(parts[falseIndex - 2].Trim(), out var bo) && bo > 0)
                buyoutCopper = bo;

            if (falseIndex >= 5 && long.TryParse(parts[falseIndex - 4].Trim(), out var mb) && mb > 0)
                minBidCopper = mb;

            if (buyoutCopper <= 0)
                buyoutCopper = minBidCopper;

            if (buyoutCopper <= 0)
            {
                rowsFailed++;
                continue;
            }

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

        var ts = scanTimeUtc ?? DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Retención 72h: borrar history viejo
        var cutoff = DateTime.UtcNow.AddHours(-72);
        await db.PriceHistory.Where(x => x.TimestampUtc < cutoff).ExecuteDeleteAsync(ct);

        var ids = perItemPricesGold.Keys.ToList();
        var urls = ids.Select(id => $"https://ah.nerfed.net/item/index?id={id}&faction=1&realm=17").ToList();

        var existingByUrl = await db.Items
            .Where(x => urls.Contains(x.Url))
            .ToDictionaryAsync(x => x.Url, ct);

        var existingByItemId = await db.Items
            .Where(x => x.WowItemId != null && ids.Contains(x.WowItemId.Value))
            .ToDictionaryAsync(x => x.WowItemId!.Value, ct);

        int itemsCreated = 0, itemsUpdated = 0, historyInserted = 0;

        await db.PriceHistory.Where(x => x.TimestampUtc == ts).ExecuteDeleteAsync(ct);

        foreach (var id in ids)
        {
            var prices = perItemPricesGold[id];
            if (prices.Count == 0) continue;

            prices.Sort();
            var median = MedianSorted(prices);
            var min = prices[0];
            var qtySum = perItemQty.TryGetValue(id, out var qq) ? qq : prices.Count;

            var url = $"https://ah.nerfed.net/item/index?id={id}&faction=1&realm=17";

            if (!existingByItemId.TryGetValue(id, out var item))
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
                db.Items.Add(item);
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

            db.PriceHistory.Add(new PriceHistory
            {
                Item = item,
                TimestampUtc = ts,
                MedianBuyoutGold = median,
                MinBuyoutGold = min,
                Qty = qtySum
            });
            historyInserted++;
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Scan OK ts={ts} parsed={parsed} failed={failed} items+={ic} items~={iu} history+={hi}",
            ts, rowsParsed, rowsFailed, itemsCreated, itemsUpdated, historyInserted);
    }

    private static decimal MedianSorted(List<decimal> sorted)
    {
        var n = sorted.Count;
        if (n == 0) return 0;
        var mid = n / 2;
        return (n % 2 == 1) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
    }
}
