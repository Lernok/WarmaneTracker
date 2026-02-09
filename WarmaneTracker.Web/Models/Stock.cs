namespace WarmaneTracker.Web.Models;

public class Stock
{
    public int Id { get; set; }           // PK propio
    public int ItemId { get; set; }       // FK único
    public Item Item { get; set; } = null!;

    public int OnHand { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
