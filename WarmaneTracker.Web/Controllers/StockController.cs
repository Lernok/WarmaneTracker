using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class StockController : Controller
{
    private readonly AppDbContext _db;
    public StockController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var rows = await _db.Items.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new StockRowVm
            {
                ItemId = x.Id,
                Name = x.Name,
                Url = x.Url,
                OnHand = x.Stock != null ? x.Stock.OnHand : 0
            })
            .ToListAsync();

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int onHand)
    {
        var item = await _db.Items.Include(x => x.Stock).FirstOrDefaultAsync(x => x.Id == itemId);
        if (item is null) return RedirectToAction(nameof(Index));

        if (item.Stock is null)
        {
            item.Stock = new Stock { ItemId = item.Id, OnHand = Math.Max(0, onHand), UpdatedAtUtc = DateTime.UtcNow };
            _db.Stock.Add(item.Stock);
        }
        else
        {
            item.Stock.OnHand = Math.Max(0, onHand);
            item.Stock.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public class StockRowVm
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public int OnHand { get; set; }
    }
}
