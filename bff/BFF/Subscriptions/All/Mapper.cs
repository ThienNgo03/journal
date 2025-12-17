using Azure.Storage.Blobs.Models;
using Bogus;

namespace BFF.Subscriptions.All;

public interface IMapper
{
    void SetMonth(Item items);
    void SetCompany(List<AppUsage> items);
    void SetIcon(List<AppUsage> items);
    void SetSubscription(List<AppUsage> items);
    void SetUsagePercent(List<AppUsage> items);
    void SetPrice(List<AppUsage> items);
    void SetDiscount(List<AppUsage> items);
    void SetHex(List<AppUsage> items);
    void SetDayLeft(List<AppUsage> items);
    void SetIsPaid(List<AppUsage> items);
    void SetIsDiscountApplied(List<AppUsage> items);
    void SetIsDiscountAvailable(List<AppUsage> items);
    void SetCustomBrush(List<string> pallete, List<AppUsage> items);
    void SetDiscountedPrice(List<AppUsage> items);

}
public class Mapper: IMapper
{
    private readonly Faker faker;
    public Mapper()
    {
        faker = new Faker();
    }

    public void SetMonth(Item items)
    {
        var today = DateTime.Today;
        items.Month = today.ToString("MMMM");
    }

    public void SetCompany(List<AppUsage> items)
    {
        //set company using faker
        foreach (var item in items)
        {
            item.Company = faker.Company.CompanyName();
        }

    }

    public void SetDayLeft(List<AppUsage> items)
    {
        //set day left using faker
        int dayleft;
        foreach (var item in items)
        {
            dayleft= faker.Random.Number(1, 31);
            item.DayLeft = $"{dayleft} day(s) left";
        }
    }

    public void SetDiscount(List<AppUsage> items)
    {
        foreach (var item in items)
        {
            var usePercentage = faker.Random.Bool();
            decimal discountValue;
            decimal finalPrice;

            if (usePercentage)
            {
                var percent = faker.Random.Decimal(0, 100);
                discountValue = item.Price * percent / 100;
            }
            else
            {
                discountValue = faker.Random.Decimal(0, item.Price);
            }

            // format "$10.99 (-15$)"
            finalPrice = item.Price - discountValue;
            item.Discount = $"${finalPrice:F2} (-{discountValue:F2}$)";
        }
    }

    public void SetDiscountedPrice(List<AppUsage> items)
    {
        foreach (var item in items)
        {
            var usePercentage = faker.Random.Bool();
            decimal discountValue;
            decimal finalPrice;

            if (usePercentage)
            {
                var percent = faker.Random.Decimal(0, 100);
                discountValue = item.Price * percent / 100;
            }
            else
            {
                discountValue = faker.Random.Decimal(0, item.Price);
            }

            // format "$10.99 (-15$)"
            finalPrice = item.Price - discountValue;
            item.DiscountedPrice = discountValue;
        }
    }


    public void SetHex(List<AppUsage> items)
    {
        //set hex color using faker
        foreach (var item in items)
        {
            item.Hex = faker.Internet.Color();
        }
    }

    public void SetIcon(List<AppUsage> items)
    {
        //set icon using faker and https://picsum.photos
        foreach (var item in items)
        {
            var imgId = faker.Random.Number(1, 1000);
            item.Icon = $"https://picsum.photos/id/{imgId}/200/200";
        }
    }

    public void SetIsDiscountApplied(List<AppUsage> items)
    {
        //set is discount applied randomly
        foreach (var item in items)
        {
            item.IsDiscountApplied = faker.Random.Bool();
        }
    }

    public void SetIsPaid(List<AppUsage> items)
    {
        //set is paid randomly
        foreach (var item in items)
        {
            item.IsPaid = faker.Random.Bool();
        }
    }

    public void SetPrice(List<AppUsage> items)
    {
        //set price using faker
        foreach (var item in items)
        {
            item.Price = faker.Finance.Amount(5, 100);
        }
    }

    public void SetSubscription(List<AppUsage> items)
    {
        //set subscription using faker
        foreach (var item in items)
        {
            item.Subscription = faker.Commerce.ProductName();
        }
    }

    public void SetUsagePercent(List<AppUsage> items)
    {
        //set usage percent using faker
        foreach (var item in items)
        {
            item.UsagePercent = faker.Random.Double(0, 100);
        }
    }

    public void SetCustomBrush(List<string> pallete, List<AppUsage> items)
    {
        //set custom brush using faker
        pallete.AddRange(items.Select(r => r.Hex).ToList());
    }

    public void SetIsDiscountAvailable(List<AppUsage> items)
    {
        foreach (var item in items)
        {
            item.IsDiscountAvailable = item.Discount != null & !item.IsDiscountApplied;
        }
    }

}
