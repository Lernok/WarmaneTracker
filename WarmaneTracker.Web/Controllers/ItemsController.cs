using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;
using WarmaneTracker.Web.Services;

namespace WarmaneTracker.Web.Controllers;

public class ItemsController : Controller
{
    private readonly AppDbContext _db;

    public ItemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 50, string? q = null, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;

        var since = DateTime.UtcNow.AddHours(-72);

        var query = _db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            // búsqueda simple (Postgres: ILike; si no compila, lo cambiamos a Like)
            query = query.Where(i =>
                (i.Name != null && EF.Functions.Like(i.Name, $"%{q}%")) ||
                (i.WowItemId != null && i.WowItemId.ToString() == q));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var pageItemIds = items.Select(x => x.Id).ToList();

        var history = await _db.PriceHistory.AsNoTracking()
            .Where(x => pageItemIds.Contains(x.ItemId) && x.TimestampUtc >= since)
            .ToListAsync(ct);

        var median72 = history
            .GroupBy(x => x.ItemId)
            .ToDictionary(
                g => g.Key,
                g => WarmaneTracker.Web.Services.Stats.Median(g.Select(x => x.MedianBuyoutGold))
            );

        ViewBag.Median72 = median72;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.Query = q;

        return View(items);
    }



    public IActionResult Create() => View(new Item());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item)
    {
        if (!ModelState.IsValid) return View(item);

        _db.Items.Add(item);
        try
        {
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se pudo guardar. ¿URL duplicada?");
            return View(item);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Item item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid) return View(item);

        var dbItem = await _db.Items.FindAsync(id);
        if (dbItem is null) return NotFound();

        // Solo campos editables
        dbItem.Name = item.Name;
        dbItem.Url = item.Url;
        dbItem.Faction = item.Faction;
        dbItem.Realm = item.Realm;
        dbItem.TargetStock = item.TargetStock;

        try
        {
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se pudo guardar. ¿URL duplicada?");
            return View(item);
        }
    }


    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(int id, [FromServices] AhScraper scraper, CancellationToken ct)
    {
        var item = await _db.Items.FindAsync(new object[] { id }, ct);
        if (item is null) return RedirectToAction(nameof(Index));

        try
        {
            var snap = await scraper.FetchAsync(item.Url, ct);

            item.LastMedianBuyoutGold = snap.Median;
            item.LastMinBuyoutGold = snap.Min;
            item.LastQty = snap.Qty;
            item.LastFetchedAtUtc = DateTime.UtcNow;

            _db.PriceHistory.Add(new PriceHistory
            {
                ItemId = item.Id,
                TimestampUtc = DateTime.UtcNow,
                MedianBuyoutGold = snap.Median,
                MinBuyoutGold = snap.Min,
                Qty = snap.Qty
            });

            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "OK: refrescado";
        }
        catch (Exception ex)
        {
            TempData["Msg"] = "ERROR: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> History(int id)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var history = await _db.PriceHistory
            .AsNoTracking()
            .Where(x => x.ItemId == id)
            .OrderByDescending(x => x.TimestampUtc)
            .Take(200)
            .ToListAsync();

        ViewBag.Item = item;
        return View(history);
    }

}
