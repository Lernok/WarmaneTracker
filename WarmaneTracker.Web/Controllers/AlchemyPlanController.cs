using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;

namespace WarmaneTracker.Web.Controllers;

public class AlchemyPlanController : Controller
{
    private readonly AppDbContext _db;
    public AlchemyPlanController(AppDbContext db) => _db = db;

    [HttpGet("/alchemy")]
    public async Task<IActionResult> Index(int step = 1, CancellationToken ct = default)
    {
        var plan = await _db.ProfessionPlans
            .AsNoTracking()
            .Where(p => p.Profession == "Alchemy" && p.Realm == 17 && p.Faction == 1)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (plan is null)
        {
            TempData["Msg"] = "No hay plan de Alchemy cargado.";
            return RedirectToAction("Index", "Home");
        }

        var stepsCount = await _db.PlanSteps.AsNoTracking()
            .Where(s => s.ProfessionPlanId == plan.Id)
            .CountAsync(ct);

        if (stepsCount == 0)
        {
            TempData["Msg"] = "El plan existe pero no tiene pasos.";
            return RedirectToAction("Index", "Home");
        }

        if (step < 1) step = 1;
        if (step > stepsCount) step = stepsCount;

        var stepEntity = await _db.PlanSteps
            .AsNoTracking()
            .Include(s => s.Reagents)
            .Where(s => s.ProfessionPlanId == plan.Id && s.Order == step)
            .FirstOrDefaultAsync(ct);

        if (stepEntity is null)
        {
            TempData["Msg"] = $"No se encontró el paso {step}.";
            return RedirectToAction(nameof(Index), new { step = 1 });
        }

        // Traer precios para reagentes (por WowItemId)
        var reagentIds = stepEntity.Reagents.Select(r => r.WowItemId).Distinct().ToList();

        var items = await _db.Items.AsNoTracking()
            .Where(i => i.WowItemId != null && reagentIds.Contains(i.WowItemId.Value))
            .ToListAsync(ct);

        // Traer Median72h usando tu historial (PriceHistory)
        var cutoff = DateTime.UtcNow.AddHours(-72);

        var median72 = await _db.PriceHistory.AsNoTracking()
            .Where(h => h.TimestampUtc >= cutoff && h.Item.WowItemId != null && reagentIds.Contains(h.Item.WowItemId.Value))
            .GroupBy(h => h.Item.WowItemId!.Value)
            .Select(g => new
            {
                WowItemId = g.Key,
                // promedio para MVP (luego lo cambiamos por mediana real si querés)
                Avg = g.Average(x => x.MinBuyoutGold)
            })
            .ToDictionaryAsync(x => x.WowItemId, x => (decimal)x.Avg, ct);

        ViewBag.PlanName = plan.Name;
        ViewBag.Step = step;
        ViewBag.StepsCount = stepsCount;
        ViewBag.ItemsByWowId = items.ToDictionary(i => i.WowItemId!.Value, i => i);
        ViewBag.Median72ByWowId = median72;

        return View(stepEntity);
    }
}
