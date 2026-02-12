namespace WarmaneTracker.Web.Models;

public class PlanStep
{
    public int Id { get; set; }

    public int ProfessionPlanId { get; set; }
    public ProfessionPlan ProfessionPlan { get; set; } = null!;

    public int Order { get; set; }

    public int FromSkill { get; set; }
    public int ToSkill { get; set; }

    public string RecipeName { get; set; } = "";

    // ItemId del resultado del craft (WowItemId), opcional por ahora
    public int? CraftWowItemId { get; set; }

    // Cantidad a craftear en este paso
    public int CraftCount { get; set; }

    public List<StepReagent> Reagents { get; set; } = new();
    public List<PlanStepNote> Notes { get; set; } = new();

}
