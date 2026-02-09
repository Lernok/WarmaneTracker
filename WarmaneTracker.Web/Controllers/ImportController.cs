using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class ImportController : Controller
{
    private readonly AppDbContext _db;
    public ImportController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string csv)
    {
        // formato: Name,Url,TargetStock
        // ejemplo:
        // Peacebloom,https://ah.nerfed.net/item/index?id=2447&faction=1&realm=17,200

        var lines = (csv ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var line in lines)
        {
            var parts = line.Split(',', 3);
            if (parts.Length < 2)
            {
                errors.Add($"Línea inválida: {line}");
                continue;
            }

            var name = parts[0].Trim();
            var url = parts[1].Trim();
            var target = 0;

            if (parts.Length == 3 && !int.TryParse(parts[2].Trim(), out target))
            {
                errors.Add($"TargetStock inválido: {line}");
                continue;
            }

            var existing = await _db.Items.FirstOrDefaultAsync(x => x.Url == url);
            if (existing is null)
            {
                _db.Items.Add(new Item
                {
                    Name = name,
                    Url = url,
                    TargetStock = Math.Max(0, target),
                    Faction = 1,
                    Realm = 17
                });
                created++;
            }
            else
            {
                existing.Name = name;
                existing.TargetStock = Math.Max(0, target);
                updated++;
            }
        }

        await _db.SaveChangesAsync();

        ViewBag.Created = created;
        ViewBag.Updated = updated;
        ViewBag.Errors = errors;

        return View();
    }
}
