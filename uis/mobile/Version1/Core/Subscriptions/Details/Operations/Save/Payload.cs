using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Subscriptions.Details.Save;

public class Payload
{
    public string UserId { get; set; } = string.Empty;
    public string OldCompany { get; set; } = string.Empty;
    public string OldSubscription { get; set; } = string.Empty;
    public string NewCompany { get; set; } = string.Empty;
    public string NewSubscription { get; set; } = string.Empty;
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
