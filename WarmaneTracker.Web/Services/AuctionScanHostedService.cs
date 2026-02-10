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

    // Tu URL (Onyxia id=17, faction=1)
    private const string ScanUrl = "https://ah.nerfed.net/realm/getfile?id=17&faction=1";

    // Regex actual (la misma lógica que ya te funciona)
    private static readonly Regex RopeEntryRegex = new(
        @"\|Hitem:(\d+):[^|]*\|h\[.*?\]\|h\|r"",\s*\d+,\s*""[^""]+"",\s*""[^""]+"",\s*\d+,\s*(\d+),\s*(\d+),\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.Singleline);

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

        // parse (igual que tu controller, pero sobre string)
        var perItemPricesGold = new Dictionary<int, List<decimal>>();
        var perItemQty = new Dictionary<int, int>();

        int rowsParsed = 0, rowsFailed = 0, linesWithHitem = 0;
        DateTime? scanTimeUtc = null;

        using var sr = new StringReader(text);
        string? line;
        while ((line = await sr.ReadLineAsync()) is not null)
        {
            if (scanTimeUtc is null)
            {
                var tm = Regex.Match(line, @"(ImageUpdated|LastFullScan)\s*=\s*(\d+)");
                if (tm.Success && long.TryParse(tm.Groups[2].Value, out var unix))
                    scanTimeUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }

            if (!line.Contains("|Hitem:", StringComparison.OrdinalIgnoreCase))
                continue;

            linesWithHitem++;

            var normalized = line.Replace("\\\"", "\"");
            var matches = RopeEntryRegex.Matches(normalized);
            if (matches.Count == 0) continue;

            foreach (Match m in matches)
            {
                if (!int.TryParse(m.Groups[1].Value, out var itemId)) { rowsFailed++; continue; }
                if (!long.TryParse(m.Groups[2].Value, out var buyoutCopper) || buyoutCopper <= 0) { rowsFailed++; continue; }
                if (!int.TryParse(m.Groups[3].Value, out var stack) || stack <= 0) stack = 1;

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

        var ts = scanTimeUtc ?? DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=60000;", ct);
        // Retención 72h: borrar history viejo
        var cutoff = DateTime.UtcNow.AddHours(-72);
        await db.PriceHistory.Where(x => x.TimestampUtc < cutoff).ExecuteDeleteAsync(ct);

        var ids = perItemPricesGold.Keys.ToList();
        var urls = ids.Select(id => $"https://ah.nerfed.net/item/index?id={id}&faction=1&realm=17").ToList();

        var existingByUrl = await db.Items.Where(x => urls.Contains(x.Url)).ToDictionaryAsync(x => x.Url, ct);
        var existingByItemId = await db.Items.Where(x => x.WowItemId != null && ids.Contains(x.WowItemId.Value))
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

            // snapshot
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

        _logger.LogInformation("Scan OK ts={ts} linesWithHitem={lines} parsed={parsed} failed={failed} items+={ic} items~={iu} history+={hi}",
            ts, linesWithHitem, rowsParsed, rowsFailed, itemsCreated, itemsUpdated, historyInserted);
    }

    private static decimal MedianSorted(List<decimal> sorted)
    {
        var n = sorted.Count;
        if (n == 0) return 0;
        var mid = n / 2;
        return (n % 2 == 1) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
    }
}
