using Bogus;

namespace BFF.Subscriptions.All;

public interface IMapper
{
    void SetMonth(Item responses);
    void SetCompany(List<AppUsage> responses);
    void SetIcon(List<AppUsage> responses);
    void SetSubscription(List<AppUsage> responses);
    void SetUsagePercent(List<AppUsage> responses);
    void SetPrice(List<AppUsage> responses);
    void SetDiscount(List<AppUsage> responses);
    void SetHex(List<AppUsage> responses);
    void SetDayLeft(List<AppUsage> responses);
    void SetIsPaid(List<AppUsage> responses);
    void SetIsDiscountApplied(List<AppUsage> responses);
    void SetCustomBrush(List<string> responses);

}
public class Mapper:IMapper
{
    private readonly Faker faker;
    public Mapper()
    {
        faker = new Faker();
    }

    public void SetMonth(Item responses)
    {
        var today = DateTime.Today;
        responses.Month = today.ToString("MMMM");
    }

    public void SetCompany(List<AppUsage> responses)
    {
        //set company using faker
        foreach (var response in responses)
        {
            response.Company = faker.Company.CompanyName();
        }
    }

    public void SetDayLeft(List<AppUsage> responses)
    {
        //set day left using faker
        int dayleft;
        foreach (var response in responses)
        {
            dayleft= faker.Random.Number(1, 31);
            response.DayLeft = $"{dayleft} day(s) left";
        }
    }

    public void SetDiscount(List<AppUsage> responses)
    {
        throw new NotImplementedException();
    }

    public void SetHex(List<AppUsage> responses)
    {
        //set hex color using faker
        foreach (var response in responses)
        {
            response.Hex = faker.Internet.Color();
        }
    }

    public void SetIcon(List<AppUsage> responses)
    {
        //set icon using faker and https://picsum.photos
        foreach (var response in responses)
        {
            var imgId = faker.Random.Number(1, 1000);
            response.Icon = $"https://picsum.photos/id/{imgId}/200/200";
        }
    }

    public void SetIsDiscountApplied(List<AppUsage> responses)
    {
        //set is discount applied randomly
        foreach (var response in responses)
        {
            response.IsDiscountApplied = faker.Random.Bool();
        }
    }

    public void SetIsPaid(List<AppUsage> responses)
    {
        //set is paid randomly
        foreach (var response in responses)
        {
            response.IsPaid = faker.Random.Bool();
        }
    }

    public void SetPrice(List<AppUsage> responses)
    {
        //set price using faker
        foreach (var response in responses)
        {
            response.Price = faker.Finance.Amount(5, 100);
        }
    }

    public void SetSubscription(List<AppUsage> responses)
    {
        //set subscription using faker
        foreach (var response in responses)
        {
            response.Subscription = faker.Commerce.ProductName();
        }
    }

    public void SetUsagePercent(List<AppUsage> responses)
    {
        //set usage percent using faker
        foreach (var response in responses)
        {
            response.UsagePercent = faker.Random.Double(0, 100);
        }
    }

    public void SetCustomBrush(List<string> responses)
    {
        //set custom brush using faker
        for (int i = 0; i < responses.Count; i++)
        {
            responses[i] = faker.Internet.Color();
        }
    }
}
