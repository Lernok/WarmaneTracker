namespace WarmaneTracker.Web.Models;

public class PriceHistory
{
    public long Id { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public decimal MedianBuyoutGold { get; set; }
    public decimal MinBuyoutGold { get; set; }
    public int Qty { get; set; }
}
