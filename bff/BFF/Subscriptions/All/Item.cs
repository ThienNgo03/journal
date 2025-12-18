namespace BFF.Subscriptions.All;

public class Item
{
    public string Month { get; set; } = string.Empty;
    public List<AppUsage> AppUsages { get; set; } = [];
    public List<string> CustomBrush { get; set; } = [];
}
public class AppUsage
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Company { get; set; }= string.Empty;
    public string Icon { get; set; }= string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public double UsagePercent { get; set; }
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
