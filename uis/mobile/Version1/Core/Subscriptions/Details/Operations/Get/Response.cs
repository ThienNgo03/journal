namespace Core.Subscriptions.Details.Get;

public class Response
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Discount { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string Hex { get; set; } = string.Empty;
    public DateTime RenewalDate { get; set; }
    public bool IsRecursive { get; set; }
    public bool? IsDiscountApplied { get; set; }
    public bool? IsDiscountAvailable { get; set; }
}
