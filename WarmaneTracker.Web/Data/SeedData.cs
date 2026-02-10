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
            Name = "WotLK Alchemy",
            Realm = 17,
            Faction = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        // MVP (10 pasos iniciales) — después lo refinamos con la guía final
        // IDs: Peacebloom=2447, Silverleaf=765, Empty Vial=3371
        plan.Steps.AddRange(new[]
{
    Step(1, 1, 60, "Minor Healing Potion", craftWowItemId: 118, craftCount: 65,
        reagents: new[] { R(2447,"Peacebloom",65), R(765,"Silverleaf",65), R(3371,"Empty Vial",65) }),

    Step(2, 60, 110, "Lesser Healing Potion", craftWowItemId: 858, craftCount: 65,
        reagents: new[] { R(118,"Minor Healing Potion",65), R(2450,"Briarthorn",65) }),

    Step(3, 110, 140, "Healing Potion", craftWowItemId: 929, craftCount: 35,
        reagents: new[] { R(2453,"Bruiseweed",35), R(2450,"Briarthorn",35), R(3372,"Leaded Vial",35) }),

    Step(4, 140, 155, "Lesser Mana Potion", craftWowItemId: 3385, craftCount: 20,
        reagents: new[] { R(785,"Mageroyal",20), R(3820,"Stranglekelp",20), R(3371,"Empty Vial",20) }),

    Step(5, 155, 185, "Greater Healing Potion", craftWowItemId: 1710, craftCount: 35,
        reagents: new[] { R(3357,"Liferoot",35), R(3356,"Kingsblood",35), R(3372,"Leaded Vial",35) }),

    Step(6, 185, 210, "Elixir of Agility", craftWowItemId: 8949, craftCount: 30,
        reagents: new[] { R(3820,"Stranglekelp",30), R(3821,"Goldthorn",30), R(3372,"Leaded Vial",30) }),

    Step(7, 210, 215, "Elixir of Greater Defense", craftWowItemId: 8951, craftCount: 5,
        reagents: new[] { R(3355,"Wild Steelbloom",5), R(3821,"Goldthorn",5), R(3372,"Leaded Vial",5) }),

    Step(8, 215, 230, "Superior Healing Potion", craftWowItemId: 3928, craftCount: 15,
        reagents: new[] { R(8838,"Sungrass",15), R(3358,"Khadgar's Whisker",15), R(8925,"Crystal Vial",15) }),

    Step(9, 230, 231, "Philosopher's Stone", craftWowItemId: 11459, craftCount: 1,
        reagents: new[] { R(3575,"Iron Bar",4), R(9262,"Black Vitriol",1), R(8831,"Purple Lotus",4), R(4625,"Firebloom",4) }),

    Step(10, 231, 265, "Elixir of Detect Undead", craftWowItemId: 9154, craftCount: 45,
        reagents: new[] { R(8836,"Arthas' Tears",45), R(8925,"Crystal Vial",45) }),

    Step(11, 265, 285, "Superior Mana Potion", craftWowItemId: 13443, craftCount: 30,
        reagents: new[] { R(8838,"Sungrass",60), R(8839,"Blindweed",60), R(8925,"Crystal Vial",30) }),

    Step(12, 285, 300, "Major Healing Potion", craftWowItemId: 13446, craftCount: 20,
        reagents: new[] { R(13464,"Golden Sansam",40), R(13465,"Mountain Silversage",20), R(8925,"Crystal Vial",20) }),

    // 13A / 13B (cargamos ambas como pasos consecutivos)
    Step(13, 300, 310, "Volatile Healing Potion (A)", craftWowItemId: 33732, craftCount: 15,
        reagents: new[] { R(13464,"Golden Sansam",15), R(22785,"Felweed",15), R(18256,"Imbued Vial",15) }),

    Step(14, 300, 310, "Adept's Elixir (B)", craftWowItemId: 33740, craftCount: 15,
        reagents: new[] { R(13463,"Dreamfoil",15), R(22785,"Felweed",15), R(18256,"Imbued Vial",15) }),

    Step(15, 310, 325, "Elixir of Healing Power", craftWowItemId: 28545, craftCount: 25,
        reagents: new[] { R(13464,"Golden Sansam",25), R(22786,"Dreaming Glory",25), R(18256,"Imbued Vial",25) }),

    Step(16, 325, 335, "Elixir of Draenic Wisdom", craftWowItemId: 39638, craftCount: 5,
        reagents: new[] { R(22785,"Felweed",5), R(22789,"Terocone",5), R(18256,"Imbued Vial",5) }),

    Step(17, 335, 340, "Super Healing Potion", craftWowItemId: 28551, craftCount: 5,
        reagents: new[] { R(22785,"Felweed",5), R(22791,"Netherbloom",10), R(18256,"Imbued Vial",5) }),

    Step(18, 340, 350, "Super Mana Potion", craftWowItemId: 28555, craftCount: 10,
        reagents: new[] { R(22785,"Felweed",10), R(22786,"Dreaming Glory",20), R(18256,"Imbued Vial",10) }),

    Step(19, 350, 360, "Resurgent Healing Potion", craftWowItemId: 53838, craftCount: 10,
        reagents: new[] { R(36901,"Goldclover",20), R(18256,"Imbued Vial",10) }),

    Step(20, 360, 365, "Icy Mana Potion", craftWowItemId: 53839, craftCount: 5,
        reagents: new[] { R(36907,"Talandra's Rose",10), R(18256,"Imbued Vial",5) }),

    Step(21, 365, 375, "Spellpower Elixir", craftWowItemId: 53842, craftCount: 10,
        reagents: new[] { R(36901,"Goldclover",10), R(36904,"Tiger Lily",10), R(18256,"Imbued Vial",10) }),

    Step(22, 375, 380, "Pygmy Oil", craftWowItemId: 53812, craftCount: 5,
        reagents: new[] { R(40199,"Pygmy Suckerfish",5) }),

    Step(23, 380, 385, "Potion of Nightmares", craftWowItemId: 53900, craftCount: 5,
        reagents: new[] { R(36901,"Goldclover",5), R(36907,"Talandra's Rose",10), R(18256,"Imbued Vial",5) }),

    Step(24, 385, 395, "Elixir of Mighty Strength", craftWowItemId: 54218, craftCount: 12,
        reagents: new[] { R(36904,"Tiger Lily",24), R(18256,"Imbued Vial",12) }),

    Step(25, 395, 400, "Elixir of Mighty Agility", craftWowItemId: 53840, craftCount: 5,
        reagents: new[] { R(36901,"Goldclover",10), R(36903,"Adder's Tongue",10), R(18256,"Imbued Vial",5) }),

    Step(26, 400, 401, "Northrend Alchemy Research", craftWowItemId: 60893, craftCount: 1,
        reagents: new[] { R(36901,"Goldclover",10), R(36903,"Adder's Tongue",10), R(36907,"Talandra's Rose",4), R(40411,"Enchanted Vial",4) }),

    // 26A / 26B
    Step(27, 401, 405, "Elixir of Mighty Agility (A)", craftWowItemId: 53840, craftCount: 7,
        reagents: new[] { R(36901,"Goldclover",14), R(36903,"Adder's Tongue",14), R(18256,"Imbued Vial",7) }),

    Step(28, 401, 405, "Elixir of Mighty Thoughts (B)", craftWowItemId: 60367, craftCount: 7,
        reagents: new[] { R(37921,"Deadnettle",14), R(36907,"Talandra's Rose",7), R(18256,"Imbued Vial",7) }),

    Step(29, 405, 415, "Indestructible Potion", craftWowItemId: 53905, craftCount: 10,
        reagents: new[] { R(36906,"Icethorn",20), R(18256,"Imbued Vial",10) }),

    Step(30, 415, 425, "Runic Mana Potion", craftWowItemId: 53837, craftCount: 30,
        reagents: new[] { R(36905,"Lichbloom",60), R(36901,"Goldclover",30), R(18256,"Imbued Vial",30) }),

    Step(31, 425, 430, "Transmute: Titanium", craftWowItemId: 60350, craftCount: 7,
        reagents: new[] { R(36913,"Saronite Bar",56) }),

    // 30A / 30B
    Step(32, 430, 435, "Transmute: Earthsiege Diamond (A)", craftWowItemId: 57427, craftCount: 5,
        reagents: new[] { R(36932,"Dark Jade",5), R(36929,"Huge Citrine",5), R(36860,"Eternal Fire",5) }),

    Step(33, 430, 435, "Transmute: Skyflare Diamond (B)", craftWowItemId: 57425, craftCount: 5,
        reagents: new[] { R(36917,"Bloodstone",5), R(36923,"Chalcedony",5), R(35623,"Eternal Air",5) }),

    // 31A..31D
    Step(34, 435, 450, "Flask of Endless Rage (A)", craftWowItemId: 53903, craftCount: 15,
        reagents: new[] { R(36905,"Lichbloom",105), R(36901,"Goldclover",45), R(36908,"Frost Lotus",15), R(40411,"Enchanted Vial",15) }),

    Step(35, 435, 450, "Flask of Pure Mojo (B)", craftWowItemId: 54213, craftCount: 15,
        reagents: new[] { R(36906,"Icethorn",105), R(40195,"Pygmy Suckerfish",45), R(36908,"Frost Lotus",15), R(40411,"Enchanted Vial",15) }),

    Step(36, 435, 450, "Flask of Stoneblood (C)", craftWowItemId: 53902, craftCount: 15,
        reagents: new[] { R(36905,"Lichbloom",105), R(37704,"Crystallized Life",45), R(36908,"Frost Lotus",15), R(40411,"Enchanted Vial",15) }),

    Step(37, 435, 450, "Flask of the Frost Wyrm (D)", craftWowItemId: 53901, craftCount: 15,
        reagents: new[] { R(36905,"Lichbloom",75), R(36906,"Icethorn",75), R(36908,"Frost Lotus",15), R(40411,"Enchanted Vial",15) }),
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
