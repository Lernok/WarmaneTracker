using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class ItemsController : Controller
{
    private readonly AppDbContext _db;

    public ItemsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var items = await _db.Items.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
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

        _db.Entry(item).State = EntityState.Modified;

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
}
