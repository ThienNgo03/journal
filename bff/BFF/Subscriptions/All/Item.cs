namespace BFF.Subscriptions.All;

public class Item
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<ChartSlice> ChartSlices { get; set; } = new();
    public List<AppUsage> AppUsages { get; set; } = [];
    public List<string> CustomBrushes { get; set; } = new();
}
public class AppUsage
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Company { get; set; }= string.Empty;
    public string Icon { get; set; }= string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Discount { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string Hex { get; set; } = string.Empty;
    public string DayLeft { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public bool? IsDiscountApplied { get; set; }
    public bool? IsDiscountAvailable { get; set; }
}

public class ChartSlice
{
    public string Subscription { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
