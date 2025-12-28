using BFF.Authentication.Register;
using BFF.Subscriptions.All;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using Spectre.Console.Rendering;
using System.Globalization;

namespace BFF.Subscriptions;

[Route("api/subscriptions")]
[ApiController]
[Authorize]
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
        var obsoleteSubscriptions = subscriptions.Where(sub => sub.RenewalDate <= DateTime.UtcNow).ToList();
        var hasObsoleteSubscriptions = obsoleteSubscriptions.Any();
        if (hasObsoleteSubscriptions)
        {
            foreach(var obsoleteSubscription in obsoleteSubscriptions)
            {
                if (obsoleteSubscription.IsRecursive == false)
                {
                    await _subscriptions.DeleteAsync(new() { Id = obsoleteSubscription.Id });
                }
                if (obsoleteSubscription.IsRecursive == true)
                {
                    DateTime newRenewalDate = obsoleteSubscription.RenewalDate;
                    do
                    {
                        newRenewalDate = new DateTime(newRenewalDate.AddMonths(1).Year,
                                                                newRenewalDate.AddMonths(1).Month,
                                                                Math.Min(newRenewalDate.Day, DateTime.DaysInMonth(newRenewalDate.AddMonths(1).Year,
                                                                                                                                    newRenewalDate.AddMonths(1).Month)));
                    } while (newRenewalDate <= DateTime.UtcNow);
                    await _subscriptions.PutAsync(new()
                    {
                        Id = obsoleteSubscription.Id,
                        UserId = obsoleteSubscription.UserId,
                        PackageId = obsoleteSubscription.PackageId,
                        Price = obsoleteSubscription.Price,
                        Currency = obsoleteSubscription.Currency,
                        ChartColor = obsoleteSubscription.ChartColor,
                        RenewalDate = newRenewalDate,
                        IsRecursive = obsoleteSubscription.IsRecursive,
                    });
                }
            }
        }
        var providersResponse = await _providers.GetAsync(new());
        var providers = providersResponse.Content?.Items;
        var packagesResponse = await _packages.GetAsync(new());
        var packages = packagesResponse.Content?.Items;
        var subscriptionByUserIdsResponse = await _subscriptionByUserIds.GetAsync(new() { UserId = userId});
        var subscriptionByUserIds = subscriptionByUserIdsResponse.Content;
        var obstoleSubscriptionByUserIds = subscriptionByUserIds.Where(sub => sub.RenewalDate <= DateTime.UtcNow).ToList();
        var hasObstoleSubscriptionByUserIds = obstoleSubscriptionByUserIds.Any();
        if (hasObstoleSubscriptionByUserIds)
        {
            foreach (var obsoleteSubscription in obstoleSubscriptionByUserIds)
            {
                if (obsoleteSubscription.IsRecursive == false)
                {
                    await _subscriptionByUserIds.DeleteAsync(new() { UserId = obsoleteSubscription.UserId, SubscriptionPlan = obsoleteSubscription.SubscriptionPlan, CompanyName = obsoleteSubscription.CompanyName });
                }
                if (obsoleteSubscription.IsRecursive == true)
                {
                    DateTime newRenewalDate = obsoleteSubscription.RenewalDate;
                    do
                    {
                        newRenewalDate = new DateTime(newRenewalDate.AddMonths(1).Year,
                                                                newRenewalDate.AddMonths(1).Month,
                                                                Math.Min(newRenewalDate.Day, DateTime.DaysInMonth(newRenewalDate.AddMonths(1).Year,
                                                                                                                                     newRenewalDate.AddMonths(1).Month)));
                    } while (newRenewalDate <= DateTime.UtcNow);
                    await _subscriptionByUserIds.PutAsync(new()
                    {
                        Id = obsoleteSubscription.Id,
                        OldUserId = obsoleteSubscription.UserId,
                        OldSubscriptionPlan = obsoleteSubscription.SubscriptionPlan,
                        OldCompanyName = obsoleteSubscription.CompanyName,
                        NewUserId = obsoleteSubscription.UserId,
                        NewSubscriptionPlan = obsoleteSubscription.SubscriptionPlan,
                        NewCompanyName = obsoleteSubscription.CompanyName,
                        Price = obsoleteSubscription.Price,
                        Currency = obsoleteSubscription.Currency,
                        ChartColor = obsoleteSubscription.ChartColor,
                        RenewalDate = newRenewalDate,
                        IsRecursive = obsoleteSubscription.IsRecursive,
                    });
                }
            }
        }
        if (hasObsoleteSubscriptions)
        {
            subscriptionsResponse = await _subscriptions.GetAsync(new() { UserId = userId });
            subscriptions = subscriptionsResponse.Content?.Items;
        }
        if (hasObstoleSubscriptionByUserIds)
        {
            subscriptionByUserIdsResponse = await _subscriptionByUserIds.GetAsync(new() { UserId = userId });
            subscriptionByUserIds = subscriptionByUserIdsResponse.Content;
        }
        Item item = new Item{};
        item.AppUsages = subscriptions.Select(sub => new AppUsage()
        {
            Id = sub.Id.ToString(),
            UserId = sub.UserId.ToString(),
            Company = providers.FirstOrDefault(pr => pr.Id == packages.FirstOrDefault(pa => pa.Id == sub.PackageId).ProviderId).Name,
            Icon = providers.FirstOrDefault(pr => pr.Id == packages.FirstOrDefault(pa => pa.Id == sub.PackageId).ProviderId).IconUrl,
            Subscription = packages.FirstOrDefault(pa => pa.Id == sub.PackageId).Name,
            Price = sub.Price,
            Currency = sub.Currency,
            Discount = null,
            DiscountedPrice = null,
            Hex = sub.ChartColor,
            DayLeft = (sub.RenewalDate - DateTime.UtcNow).Days >= 1 ? $"{(sub.RenewalDate - DateTime.UtcNow).Days} day(s) left" : $"Less than a day",
            IsPaid = DateTime.UtcNow < sub.RenewalDate,
            IsDiscountApplied = null,
            IsDiscountAvailable = null
        }).ToList();
        item.CustomBrushes = subscriptions.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).ToList().Select(sub => sub.ChartColor).ToList();

        item.ChartSlices = subscriptions.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).ToList().Select(sub => new ChartSlice()
        {
            Price = sub.Price,
            Subscription = packages.FirstOrDefault(pa => pa.Id == sub.PackageId).Name,
        }).ToList();

        Item tempItem = new Item();
        tempItem.AppUsages = subscriptionByUserIds.Select(sub => new AppUsage()
        {
            Id = sub.Id.ToString(),
            UserId = sub.UserId.ToString(),
            Company = sub.CompanyName,
            Icon = "dotnet_bot.png",
            Subscription = sub.SubscriptionPlan,
            Price = sub.Price,
            Currency = sub.Currency,
            Discount = null,
            DiscountedPrice = null,
            Hex = sub.ChartColor,
            DayLeft = (sub.RenewalDate - DateTime.UtcNow).Days >= 1 ? $"{(sub.RenewalDate - DateTime.UtcNow).Days} day(s) left" : $"Less than a day",
            IsPaid = DateTime.UtcNow < sub.RenewalDate,
            IsDiscountApplied = null,
            IsDiscountAvailable = null
        }).ToList();
        tempItem.CustomBrushes = subscriptionByUserIds.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).ToList().Select(sub => sub.ChartColor).ToList();

        tempItem.ChartSlices = subscriptionByUserIds.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).ToList().Select(sub => new ChartSlice()
        {
            Price = sub.Price,
            Subscription = sub.SubscriptionPlan,
        }).ToList();


        Item fullItem = new Item()
        {
            Month = DateTime.UtcNow.ToString("MMMM", new CultureInfo("en-US")),
            AppUsages = item.AppUsages.Concat(tempItem.AppUsages).ToList(),
            TotalPrice = subscriptions.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).Select(sub => sub.Price).Sum() + subscriptionByUserIds.Where(sub => sub.RenewalDate.Month == DateTime.UtcNow.AddMonths(1).Month).Select(sub => sub.Price).Sum(),
            ChartSlices = item.ChartSlices.Concat(tempItem.ChartSlices).ToList(),
            CustomBrushes = item.CustomBrushes.Concat(tempItem.CustomBrushes).ToList()
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

    //[HttpDelete("delete")]
    //public async Task<IActionResult> Delete([FromQuery] Delete.Parameters parameters)
    //{
    //    Guid? userId = Guid.TryParse(parameters.UserId, out Guid uId) ? uId : null;
    //    Guid? subscriptionId = Guid.TryParse(parameters.Id, out Guid id) ? id : null;
    //    var providersResponse = await _providers.GetAsync(new() { Name = parameters.CompanyName});
    //    Guid? providerId = providersResponse.Content.Items.FirstOrDefault()?.Id;
    //    var packagesResponse = await _packages.GetAsync(new() { Name = parameters.SubscriptionPlan, ProviderId = providerId });
    //    Guid? packageId = packagesResponse.Content.Items.FirstOrDefault(x => x.Name == parameters.SubscriptionPlan && x.ProviderId == providerId)?.Id;

    //    if (providerId.HasValue && packageId.HasValue)
    //    {
    //        if (!subscriptionId.HasValue)
    //        {
    //            Console.WriteLine("Null Subscription Id.");
    //            return NoContent();
    //        }
    //        await _subscriptions.DeleteAsync(new()
    //        {
    //            Id = subscriptionId.Value,
    //        });
    //        return NoContent();
    //    }
    //    await _subscriptionByUserIds.DeleteAsync(new() { UserId = userId.Value, SubscriptionPlan = parameters.SubscriptionPlan, CompanyName = parameters.CompanyName });
    //    return NoContent();
    //}
}
