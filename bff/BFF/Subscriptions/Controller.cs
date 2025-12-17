using BFF.Authentication.Register;
using BFF.Subscriptions.All;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Spectre.Console.Rendering;

namespace BFF.Subscriptions;

[Route("api/subscriptions")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly Provider.Subscriptions.IRefitInterface _subscriptions;
    private readonly Provider.Providers.IRefitInterface _providers;
    private readonly Provider.Packages.IRefitInterface _packages;
    private readonly Provider.SubscriptionByUserIds.IRefitInterface _subscriptionByUserIds;
    private readonly Provider.UserIdBySubscriptionPlans.IRefitInterface _userIdBySubscriptionPlans;
    public Controller(IMapper mapper,
                      Provider.Subscriptions.IRefitInterface subscriptions, 
                      Provider.Providers.IRefitInterface providers,
                      Provider.Packages.IRefitInterface packages,
                      Provider.SubscriptionByUserIds.IRefitInterface subscriptionByUserIds,
                      Provider.UserIdBySubscriptionPlans.IRefitInterface userIdBySubscriptionPlans)
    {
        _mapper = mapper;
        _subscriptions = subscriptions;
        _providers = providers;
        _packages = packages;
        _subscriptionByUserIds = subscriptionByUserIds;
        _userIdBySubscriptionPlans = userIdBySubscriptionPlans;
    }

    [HttpGet("all")]
    public async Task<IActionResult> All([FromQuery] All.Parameters parameters)
    {
        Guid? userId = Guid.TryParse(parameters.UserId, out Guid uId) ? uId : null;
        var subscriptionsResponse = await _subscriptions.GetAsync(new() { UserId = userId});
        var subscriptions = subscriptionsResponse.Content?.Items;
        var providersResponse = await _providers.GetAsync(new());
        var providers = providersResponse.Content?.Items;
        var packagesResponse = await _packages.GetAsync(new());
        var packages = packagesResponse.Content?.Items;
        var subscriptionByUserIdResponse = await _subscriptionByUserIds.GetAsync(new() { UserId = userId});
        var subscriptionByUserIds = subscriptionByUserIdResponse.Content;
        var monthTotalPrice = subscriptions.Where(sub => sub.PurchasedDate.Month == DateTime.UtcNow.Month).Select(sub => sub.Price).Sum() + subscriptionByUserIds.Where(sub => sub.PurchasedDate.Month == DateTime.UtcNow.Month).Select(sub => sub.Price).Sum();
        Item item = new Item{};
        item.AppUsages = subscriptions.Select(sub => new AppUsage()
        {
            Company = providers.FirstOrDefault(pr => pr.Id == packages.FirstOrDefault(pa => pa.Id == sub.PackageId).ProviderId).Name,
            Icon = providers.FirstOrDefault(pr => pr.Id == packages.FirstOrDefault(pa => pa.Id == sub.PackageId).ProviderId).IconUrl,
            Subscription = packages.FirstOrDefault(pa => pa.Id == sub.PackageId).Name,
            UsagePercent = (sub.PurchasedDate.Month == DateTime.UtcNow.Month) ? 
                               (double)((sub.Price / monthTotalPrice)*100) :
                               0,
            Price = sub.Price,
            Currency = sub.Currency,
            Discount = null,
            DiscountedPrice = null,
            Hex = sub.ChartColor,
            DayLeft = $"{(sub.RenewalDate - DateTime.UtcNow).Days} day(s) left",
            IsPaid = sub.PurchasedDate < DateTime.UtcNow && DateTime.UtcNow < sub.RenewalDate,
            IsDiscountApplied = null,
            IsDiscountAvailable = null
        }).ToList();
        item.CustomBrush = subscriptions.Select(sub => sub.ChartColor).ToList();

        Item tempItem = new Item();
        tempItem.AppUsages = subscriptionByUserIds.Select(sub => new AppUsage()
        {
            Company = sub.CompanyName,
            Icon = "dotnet_bot.png",
            Subscription = sub.SubscriptionPlan,
            UsagePercent = (sub.PurchasedDate.Month == DateTime.UtcNow.Month) ?
                               (double)((sub.Price / monthTotalPrice) * 100) :
                               0,
            Price = sub.Price,
            Currency = sub.Currency,
            Discount = null,
            DiscountedPrice = null,
            Hex = sub.ChartColor,
            DayLeft = sub.RenewalDate > DateTime.UtcNow ? $"{(sub.RenewalDate - DateTime.UtcNow).Days} day(s) left" : $"{(DateTime.UtcNow - sub.RenewalDate).Days} day(s) passed",
            IsPaid = sub.PurchasedDate < DateTime.UtcNow && DateTime.UtcNow < sub.RenewalDate,
            IsDiscountApplied = null,
            IsDiscountAvailable = null
        }).ToList();
        tempItem.CustomBrush = subscriptionByUserIds.Select(sub => sub.ChartColor).ToList();

        Item fullItem = new Item()
        {
            Month = DateTime.UtcNow.Month.ToString(),
            AppUsages = item.AppUsages.Concat(tempItem.AppUsages).ToList(),
            CustomBrush = item.CustomBrush.Concat(tempItem.CustomBrush).ToList()
        };



        // thêm dữ liệu người dùng nhập trong cassandra

        //var item = new Item{};
        //for (int i = 0; i < 5; i++) 
        //{
        //    item.AppUsages.Add(new AppUsage());
        //}

        //_mapper.All.SetMonth(item);
        //_mapper.All.SetCompany(item.AppUsages);
        //_mapper.All.SetIcon(item.AppUsages);
        //_mapper.All.SetSubscription(item.AppUsages);
        //_mapper.All.SetUsagePercent(item.AppUsages);
        //_mapper.All.SetPrice(item.AppUsages);
        //_mapper.All.SetDiscount(item.AppUsages);
        //_mapper.All.SetDiscountedPrice(item.AppUsages);
        //_mapper.All.SetHex(item.AppUsages);
        //_mapper.All.SetDayLeft(item.AppUsages);
        //_mapper.All.SetIsPaid(item.AppUsages);
        //_mapper.All.SetIsDiscountApplied(item.AppUsages);
        //_mapper.All.SetIsDiscountAvailable(item.AppUsages);
        //_mapper.All.SetCustomBrush(item.CustomBrush, item.AppUsages);

        return Ok(fullItem);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Create.Payload payload)
    {
        var providersResponse = await _providers.GetAsync(new());
        var companies = providersResponse.Content?.Items;
        var companyNames = companies.Select(pr => pr.Name.ToLower().Replace(" ", "")).ToList();
        var packagesResponse = await _packages.GetAsync(new());
        var subscriptions = packagesResponse.Content?.Items;
        var subscriptionNames = subscriptions.Select(pa => pa.Name.ToLower().Replace(" ", "")).ToList();
        bool isTempSubscription = !companyNames.Contains(payload.Company.ToLower().Replace(" ", "")) || !subscriptionNames.Contains(payload.Subscription.ToLower().Replace(" ", ""));

        if (isTempSubscription)
        {
            await _subscriptionByUserIds.PostAsync(new()
            {
                UserId = Guid.Parse(payload.UserId),
                CompanyName = payload.Company,
                SubscriptionPlan = payload.Subscription,
                Price = payload.Price,
                Currency = payload.Currency,
                ChartColor = payload.Hex,
                PurchasedDate = payload.PurchasedDate,
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return Created("", "temp-subscription-created");
        }
        var providerId = companies.FirstOrDefault(pro => pro.Name.ToLower().Replace(" ", "") == payload.Company.ToLower().Replace(" ", "")).Id;
        var packageId = subscriptions.FirstOrDefault(sub => sub.Name == payload.Subscription && sub.ProviderId == providerId).Id;
        await _subscriptions.PostAsync(new()
        {
            UserId = Guid.Parse(payload.UserId),
            PackageId = packageId,
            Price = payload.Price,
            Currency = payload.Currency,
            ChartColor = payload.Hex,
            PurchasedDate = payload.PurchasedDate,
            RenewalDate = payload.RenewalDate,
            IsRecursive = payload.IsRecursive,
        });
        return Created("", "subscription-created");
    }
}
