namespace WarmaneTracker.Web.Models;

public class ProfessionPlan
{
    public int Id { get; set; }

    // Por ahora dejamos string simple. Más adelante podemos pasar a enum.
    public string Profession { get; set; } = "Alchemy";

    public string Name { get; set; } = "WotLK Alchemy 1-450";

    public int Realm { get; set; } = 17;
    public int Faction { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PlanStep> Steps { get; set; } = new();
}
