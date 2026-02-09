namespace WarmaneTracker.Web.Services;

public static class Stats
{
    public static decimal Median(IEnumerable<decimal> values)
    {
        var arr = values.OrderBy(x => x).ToArray();
        if (arr.Length == 0) return 0;

        var mid = arr.Length / 2;
        return (arr.Length % 2 == 0)
            ? (arr[mid - 1] + arr[mid]) / 2m
            : arr[mid];
    }
}
