using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Data;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Controllers;

public class AlchemyPlanController : Controller
{
    private readonly AppDbContext _db;
    public AlchemyPlanController(AppDbContext db) => _db = db;

    [HttpGet("/alchemy")]
    public async Task<IActionResult> Index(int step = 1, CancellationToken ct = default)
    {
        // 1) Si NO vino step por query, intentamos usar cookie y redirigimos
        if (!Request.Query.ContainsKey("step"))
        {
            if (Request.Cookies.TryGetValue("alchemy_last_step", out var s) &&
                int.TryParse(s, out var last) &&
                last > 0)
            {
                return RedirectToAction(nameof(Index), new { step = last });
            }
        }

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
            .Include(s => s.Notes)
            .Where(s => s.ProfessionPlanId == plan.Id && s.Order == step)
            .FirstOrDefaultAsync(ct);



        if (stepEntity is null)
        {
            TempData["Msg"] = $"No se encontró el paso {step}.";
            return RedirectToAction(nameof(Index), new { step = 1 });
        }

        // Notes "NEXT" del paso anterior (se muestran como "Antes de este paso")
        List<PlanStepNote> prevNextNotes = new();

        if (step > 1)
        {
            prevNextNotes = await _db.PlanStepNotes
                .AsNoTracking()
                .Where(n => n.PlanStep.ProfessionPlanId == plan.Id
                            && n.PlanStep.Order == step - 1
                            && n.Scope == "NEXT")
                .OrderBy(n => n.SortOrder)
                .ToListAsync(ct);
        }

        ViewBag.PrevNextNotes = prevNextNotes;


        // 2) Guardar cookie con el paso actual (ya validado)
        Response.Cookies.Append("alchemy_last_step", step.ToString(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true,
            Path = "/"
        });

        // ---- tu lógica existente de vendor/craftable/precios ----

        var vendorReagents = stepEntity.Reagents.Where(r => r.IsVendor).ToList();

        var allSteps = await _db.PlanSteps
            .AsNoTracking()
            .Where(s => s.ProfessionPlanId == plan.Id)
            .Select(s => new { s.Order, s.CraftWowItemId })
            .ToListAsync(ct);

        var craftableWowIds = allSteps
            .Where(s => s.CraftWowItemId != null)
            .Select(s => s.CraftWowItemId!.Value)
            .ToHashSet();

        var reagentIds = stepEntity.Reagents
            .Where(r => !r.IsVendor)
            .Select(r => r.WowItemId)
            .Distinct()
            .ToList();

        var items = await _db.Items.AsNoTracking()
            .Where(i => i.WowItemId != null && reagentIds.Contains(i.WowItemId.Value))
            .ToListAsync(ct);

        var cutoff = DateTime.UtcNow.AddHours(-72);

        var median72 = await _db.PriceHistory.AsNoTracking()
            .Where(h => h.TimestampUtc >= cutoff && h.Item.WowItemId != null && reagentIds.Contains(h.Item.WowItemId.Value))
            .GroupBy(h => h.Item.WowItemId!.Value)
            .Select(g => new { WowItemId = g.Key, Avg = g.Average(x => x.MinBuyoutGold) })
            .ToDictionaryAsync(x => x.WowItemId, x => (decimal)x.Avg, ct);

        var priceGoldByWowId = new Dictionary<int, decimal>();

        foreach (var r in vendorReagents)
        {
            if ((r.VendorPriceCopper ?? 0) <= 0) continue;
            priceGoldByWowId[r.WowItemId] = (r.VendorPriceCopper!.Value / 10000m);
        }

        foreach (var wowId in reagentIds)
        {
            if (priceGoldByWowId.ContainsKey(wowId)) continue;
            if (median72.TryGetValue(wowId, out var g))
                priceGoldByWowId[wowId] = g;
        }

        ViewBag.PriceGoldByWowId = priceGoldByWowId;
        ViewBag.PlanName = plan.Name;
        ViewBag.Step = step;
        ViewBag.StepsCount = stepsCount;
        ViewBag.ItemsByWowId = items.ToDictionary(i => i.WowItemId!.Value, i => i);
        ViewBag.Median72ByWowId = median72;
        ViewBag.CraftableWowIds = craftableWowIds;

        return View(stepEntity);
    }

}
