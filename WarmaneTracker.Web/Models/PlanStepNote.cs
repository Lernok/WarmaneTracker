namespace WarmaneTracker.Web.Models;

public class PlanStepNote
{
    public int Id { get; set; }

    public int PlanStepId { get; set; }
    public PlanStep PlanStep { get; set; } = null!;

    // TRAINER / RECIPE_VENDOR / TIP / REQ
    public string Kind { get; set; } = "TIP";

    public string Text { get; set; } = "";

    public int SortOrder { get; set; } = 0;

    // opcional (para badges tipo "Requires level 20")
    public int? MinCharacterLevel { get; set; }

    public string Scope { get; set; } = "INLINE";
    // INLINE = se muestra en este paso
    // NEXT   = se muestra en el paso siguiente

}
