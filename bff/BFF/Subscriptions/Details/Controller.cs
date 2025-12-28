using BFF.Subscriptions.All;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Subscriptions.Details;

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

    [HttpGet("all-companies")]
    public async Task<IActionResult> AllCompanies()
    {
        var providersResponse = await _providers.GetAsync(new());
        var companies = providersResponse.Content.Items;
        var packagesResponse = await _packages.GetAsync(new());
        var packages = packagesResponse.Content.Items;
        List<AllCompanies.Response> responses = companies.Select(provider => new AllCompanies.Response
        {
            Id = provider.Id,
            Company = provider.Name,
            Subscriptions = packages
                .Where(package => package.ProviderId == provider.Id)
                .Select(package => new AllCompanies.Subscription
                {
                    Id = package.Id,
                    Name = package.Name,
                })
                .ToList()
        }).ToList();
        return Ok(responses);
    }

    [HttpGet("{id}/detail")]
    public async Task<IActionResult> All([FromQuery] Get.Parameters parameters, [FromRoute] Guid id)
    {
        var subscriptionsResponse = await _subscriptions.GetAsync(new() { Id = id });
        var subscription = subscriptionsResponse.Content?.Items.FirstOrDefault();
        if (subscription != null)
        {
            var packagesResponse = await _packages.GetAsync(new() {Id = subscription.PackageId});
            var package = packagesResponse.Content?.Items.FirstOrDefault();
            if (package != null)
            {
                var providersResponse = await _providers.GetAsync(new() { Id = package.ProviderId});
                var provider = providersResponse.Content?.Items.FirstOrDefault();
                if (provider != null)
                {
                    Get.Response response = new Get.Response()
                    {
                        Id = subscription.Id.ToString(),
                        UserId = subscription.UserId.ToString(),
                        Company = provider.Name,
                        Subscription = package.Name,
                        Price = subscription.Price,
                        Currency = subscription.Currency,
                        Discount = null,
                        DiscountedPrice = null,
                        Hex = subscription.ChartColor,
                        RenewalDate = subscription.RenewalDate,
                        IsRecursive = subscription.IsRecursive,
                        IsDiscountApplied = null,
                        IsDiscountAvailable = null
                    };
                    return Ok(response);
                }
            }
        }

        Guid? userId = Guid.TryParse(parameters.UserId, out Guid uId) ? uId : null;
        var subscriptionByUserIdsResponse = await _subscriptionByUserIds.GetAsync(new() { UserId = userId, SubscriptionPlan = parameters.Subscription, CompanyName = parameters.Company });
        var subscriptionByUserId = subscriptionByUserIdsResponse.Content.FirstOrDefault();
        if (subscriptionByUserId != null)
        {
            Get.Response response = new Get.Response()
            {
                Id = subscriptionByUserId.Id.ToString(),
                UserId = subscriptionByUserId.UserId.ToString(),
                Company = subscriptionByUserId.CompanyName,
                Subscription = subscriptionByUserId.SubscriptionPlan,
                Price = subscriptionByUserId.Price,
                Currency = subscriptionByUserId.Currency,
                Discount = null,
                DiscountedPrice = null,
                Hex = subscriptionByUserId.ChartColor,
                RenewalDate = subscriptionByUserId.RenewalDate,
                IsRecursive = subscriptionByUserId.IsRecursive,
                IsDiscountApplied = null,
                IsDiscountAvailable = null
            };
            return Ok(response);
        }
        return NotFound();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] Create.Payload payload)
    {
        var providersResponse = await _providers.GetAsync(new() { Name = payload.Company });
        Guid? providerId = providersResponse.Content.Items.FirstOrDefault()?.Id;
        var packagesResponse = await _packages.GetAsync(new() { Name = payload.Subscription, ProviderId = providerId });
        Guid? packageId = packagesResponse.Content.Items.FirstOrDefault(x => x.Name == payload.Subscription && x.ProviderId == providerId)?.Id;
        bool isTempSubscription = !providerId.HasValue || !packageId.HasValue;

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
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return Created("", "temp-subscription-created");
        }
        await _subscriptions.PostAsync(new()
        {
            UserId = Guid.Parse(payload.UserId),
            PackageId = packageId.Value,
            Price = payload.Price,
            Currency = payload.Currency,
            ChartColor = payload.Hex,
            RenewalDate = payload.RenewalDate,
            IsRecursive = payload.IsRecursive,
        });
        return Created("", "subscription-created");
    }

    [HttpPut("{id}/save")]
    public async Task<IActionResult> Update([FromBody] Save.Payload payload, [FromRoute] Guid id)
    {
        Guid? userId = Guid.TryParse(payload.UserId, out Guid uId) ? uId : null;
        Guid? subscriptionId = id;

        var providersResponse = await _providers.GetAsync(new() { Name = payload.OldCompany });
        Guid? oldProviderId = providersResponse.Content.Items.FirstOrDefault()?.Id;
        var packagesResponse = await _packages.GetAsync(new() { Name = payload.OldSubscription, ProviderId = oldProviderId });
        Guid? oldPackageId = packagesResponse.Content.Items.FirstOrDefault(x => x.Name == payload.OldSubscription && x.ProviderId == oldProviderId)?.Id;

        providersResponse = await _providers.GetAsync(new() { Name = payload.NewCompany });
        Guid? newProviderId = providersResponse.Content.Items.FirstOrDefault()?.Id;
        packagesResponse = await _packages.GetAsync(new() { Name = payload.NewSubscription, ProviderId = newProviderId });
        Guid? newPackageId = packagesResponse.Content.Items.FirstOrDefault(x => x.Name == payload.NewSubscription && x.ProviderId == newProviderId)?.Id;

        if (oldPackageId != null && newPackageId != null)
        {
            await _subscriptions.PutAsync(new()
            {
                Id = subscriptionId.Value,
                UserId = userId.Value,
                PackageId = newPackageId.Value,
                Price = payload.Price,
                Currency = payload.Currency,
                ChartColor = payload.Hex,
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return NoContent();
        }
        if (oldPackageId == null && newPackageId == null)
        {
            await _subscriptionByUserIds.PutAsync(new()
            {
                Id = subscriptionId.Value,
                OldUserId = userId.Value,
                OldSubscriptionPlan = payload.OldSubscription,
                OldCompanyName = payload.OldCompany,
                NewUserId = userId.Value,
                NewSubscriptionPlan = payload.NewSubscription,
                NewCompanyName = payload.NewCompany,
                Price = payload.Price,
                Currency = payload.Currency,
                ChartColor = payload.Hex,
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return NoContent();
        }

        if (oldPackageId != null && newPackageId == null)
        {
            await _subscriptions.DeleteAsync(new() { Id = subscriptionId.Value });
            await _subscriptionByUserIds.PostAsync(new()
            {
                UserId = userId.Value,
                SubscriptionPlan = payload.NewSubscription,
                CompanyName = payload.NewCompany,
                Price = payload.Price,
                Currency = payload.Currency,
                ChartColor = payload.Hex,
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return NoContent();
        }
        if (oldPackageId == null && newPackageId != null)
        {
            await _subscriptionByUserIds.DeleteAsync(new()
            {
                UserId = userId.Value,
                SubscriptionPlan = payload.OldSubscription,
                CompanyName = payload.OldCompany
            });
            await _subscriptions.PostAsync(new()
            {
                UserId = userId.Value,
                PackageId = newPackageId.Value,
                Price = payload.Price,
                Currency = payload.Currency,
                ChartColor = payload.Hex,
                RenewalDate = payload.RenewalDate,
                IsRecursive = payload.IsRecursive,
            });
            return NoContent();
        }
        return NoContent();
    }

    [HttpDelete("{id}/delete")]
    public async Task<IActionResult> Delete([FromQuery] Delete.Parameters parameters, [FromRoute] Guid id)
    {
        Guid? userId = Guid.TryParse(parameters.UserId, out Guid uId) ? uId : null;
        Guid? subscriptionId = id;
        var providersResponse = await _providers.GetAsync(new() { Name = parameters.CompanyName });
        Guid? providerId = providersResponse.Content.Items.FirstOrDefault()?.Id;
        var packagesResponse = await _packages.GetAsync(new() { Name = parameters.SubscriptionPlan, ProviderId = providerId });
        Guid? packageId = packagesResponse.Content.Items.FirstOrDefault(x => x.Name == parameters.SubscriptionPlan && x.ProviderId == providerId)?.Id;

        if (providerId.HasValue && packageId.HasValue)
        {
            if (!subscriptionId.HasValue)
            {
                Console.WriteLine("Null Subscription Id.");
                return NoContent();
            }
            await _subscriptions.DeleteAsync(new()
            {
                Id = subscriptionId.Value,
            });
            return NoContent();
        }
        await _subscriptionByUserIds.DeleteAsync(new() { UserId = userId.Value, SubscriptionPlan = parameters.SubscriptionPlan, CompanyName = parameters.CompanyName });
        return NoContent();
    }
}

