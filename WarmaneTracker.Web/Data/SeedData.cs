using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Si ya existe un plan de Alchemy para este realm/faction, no seedear de nuevo
        var exists = await db.ProfessionPlans.AnyAsync(p =>
            p.Profession == "Alchemy" && p.Realm == 17 && p.Faction == 1, ct);

        if (exists) return;

        var plan = new ProfessionPlan
        {
            Profession = "Alchemy",
            Name = "WotLK Alchemy (MVP 10 steps)",
            Realm = 17,
            Faction = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        // MVP (10 pasos iniciales) — después lo refinamos con la guía final
        // IDs: Peacebloom=2447, Silverleaf=765, Empty Vial=3371
        plan.Steps.AddRange(new[]
        {
            Step(1, 1,  60, "Minor Healing Potion", craftWowItemId: 118, craftCount: 60,
                reagents: new[] { R(2447,"Peacebloom",1), R(765,"Silverleaf",1), R(3371,"Empty Vial",1) }),

            Step(2, 60,  110, "Lesser Healing Potion", craftWowItemId: 858, craftCount: 50,
                reagents: new[] { R(2450,"Briarthorn",1), R(3371,"Empty Vial",1) }),

            Step(3, 110,  140, "Healing Potion", craftWowItemId: 929, craftCount: 30,
                reagents: new[] { R(3355,"Wild Steelbloom",1), R(3820,"Stranglekelp",1), R(3371,"Empty Vial",1) }),

            Step(4, 140,  155, "Lesser Mana Potion", craftWowItemId: 3385, craftCount: 15,
                reagents: new[] { R(785,"Mageroyal",1), R(3820,"Stranglekelp",1), R(3371,"Empty Vial",1) }),

            Step(5, 155,  185, "Greater Healing Potion", craftWowItemId: 1710, craftCount: 30,
                reagents: new[] { R(3357,"Liferoot",1), R(3355,"Wild Steelbloom",1), R(3371,"Empty Vial",1) }),

            Step(6, 185,  210, "Elixir of Agility", craftWowItemId: 8949, craftCount: 25,
                reagents: new[] { R(3821,"Goldthorn",1), R(3371,"Empty Vial",1) }),

            Step(7, 210,  235, "Elixir of Greater Defense", craftWowItemId: 8951, craftCount: 25,
                reagents: new[] { R(3818,"Fadeleaf",1), R(3371,"Empty Vial",1) }),

            Step(8, 235,  260, "Elixir of Detect Undead", craftWowItemId: 9154, craftCount: 25,
                reagents: new[] { R(8831,"Purple Lotus",1), R(8925,"Crystal Vial",1) }),

            Step(9, 260,  285, "Elixir of Greater Agility", craftWowItemId: 9187, craftCount: 25,
                reagents: new[] { R(8838,"Sungrass",1), R(8925,"Crystal Vial",1) }),

            Step(10, 285,  300, "Major Healing Potion", craftWowItemId: 13446, craftCount: 15,
                reagents: new[] { R(13464,"Golden Sansam",2), R(13463,"Dreamfoil",1), R(8925,"Crystal Vial",1) }),
        });

        db.ProfessionPlans.Add(plan);
        await db.SaveChangesAsync(ct);
    }

    private static PlanStep Step(int order, int from, int to, string recipe, int? craftWowItemId, int craftCount, StepReagent[] reagents)
    {
        var s = new PlanStep
        {
            Order = order,
            FromSkill = from,
            ToSkill = to,
            RecipeName = recipe,
            CraftWowItemId = craftWowItemId,
            CraftCount = craftCount,
        };
        foreach (var r in reagents) s.Reagents.Add(r);
        return s;
    }

    private static StepReagent R(int wowItemId, string nameHint, int qty)
        => new StepReagent { WowItemId = wowItemId, NameHint = nameHint, Qty = qty };
}
