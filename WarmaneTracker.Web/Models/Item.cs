using System.Collections;

namespace WarmaneTracker.Web.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";

    public int Faction { get; set; } = 1;
    public int Realm { get; set; } = 17;

    public decimal? LastMedianBuyoutGold { get; set; }
    public decimal? LastMinBuyoutGold { get; set; }
    public int? LastQty { get; set; }
    public DateTime? LastFetchedAtUtc { get; set; }

    public List<PriceHistory> History { get; set; } = new();
    public Stock? Stock { get; set; }
    public int TargetStock { get; set; } = 0;
    public int? WowItemId { get; set; }


}
