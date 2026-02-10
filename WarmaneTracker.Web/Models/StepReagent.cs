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

    /// <summary>
    /// Si es true, este reagent se compra a vendor (no usar AH).
    /// </summary>
    public bool IsVendor { get; set; }

    /// <summary>
    /// Precio del vendor por UNIDAD (en copper). Ej: 50 silver = 5000.
    /// Solo aplica si IsVendor=true.
    /// </summary>
    public long? VendorPriceCopper { get; set; }

}
