using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Services;

namespace WarmaneTracker.Web.Controllers;

public class ShoppingController : Controller
{
    private readonly AppDbContext _db;
    public ShoppingController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var since = DateTime.UtcNow.AddHours(-72);

        var items = await _db.Items.AsNoTracking()
            .Include(x => x.Stock)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var ids = items.Select(x => x.Id).ToList();

        var history = await _db.PriceHistory.AsNoTracking()
            .Where(x => ids.Contains(x.ItemId) && x.TimestampUtc >= since)
            .ToListAsync();

        var median72 = history
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => Stats.Median(g.Select(x => x.MedianBuyoutGold)));

        var rows = items.Select(i =>
        {
            var onHand = i.Stock?.OnHand ?? 0;
            var need = Math.Max(0, i.TargetStock - onHand);
            median72.TryGetValue(i.Id, out var m72);

            return new Row
            {
                ItemId = i.Id,
                Name = i.Name,
                Url = i.Url,
                OnHand = onHand,
                Target = i.TargetStock,
                NeedToBuy = need,
                Median72h = m72 == 0 ? (decimal?)null : m72,
                EstCost = (m72 == 0 || need == 0) ? (decimal?)null : m72 * need
            };
        })
        .Where(r => r.NeedToBuy > 0)
        .OrderByDescending(r => r.EstCost ?? 0)
        .ToList();

        return View(rows);
    }

    public class Row
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public int OnHand { get; set; }
        public int Target { get; set; }
        public int NeedToBuy { get; set; }
        public decimal? Median72h { get; set; }
        public decimal? EstCost { get; set; }
    }
}
