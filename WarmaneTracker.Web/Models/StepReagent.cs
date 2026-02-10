namespace WarmaneTracker.Web.Models;

public class StepReagent
{
    public int Id { get; set; }

    public int PlanStepId { get; set; }
    public PlanStep PlanStep { get; set; } = null!;

    // WowItemId del material requerido
    public int WowItemId { get; set; }

    public string NameHint { get; set; } = ""; // opcional (para leer más fácil)

    public int Qty { get; set; }
}
