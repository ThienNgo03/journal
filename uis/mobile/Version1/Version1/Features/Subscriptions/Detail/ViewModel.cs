
using Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Version1.Features.Subscriptions.Detail;

public partial class ViewModel : NavigationAwareBaseViewModel
{
    private bool isLoadingFromNavigation = false;
    private Core.Subscriptions.Details.IRefitInterface subscriptionDetails;

    public ViewModel(IAppNavigator appNavigator, Core.Subscriptions.Details.IRefitInterface subscriptionDetails)
        : base(appNavigator)
    {
        this.subscriptionDetails = subscriptionDetails;
        FilteredCompanies = new ObservableCollection<Provider>();
        FilteredSubscriptions = new ObservableCollection<Subscription>();
        Colors = new ObservableCollection<string>(quickColors);
    }
    #region [ Init ]

    [ObservableProperty]
    Guid? id;
    [ObservableProperty]
    string? company;
    [ObservableProperty]
    string? subscriptionPlan;

    public async Task ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Load companies từ API TRƯỚC
        var ApiCompanies = await subscriptionDetails.AllCompaniesAsync();
        companies = ApiCompanies.Content.Select(c => new Provider
        {
            Id = c.Id,
            Name = c.Company,
            Subscriptions = c.Subscriptions.Select(s => new Subscription
            {
                Id = s.Id,
                Name = s.Name
            }).ToList()
        }).ToList();

        // Gán vào FilteredCompanies
        FilteredCompanies = new ObservableCollection<Provider>(companies);

        if (query.TryGetValue("__DATA__", out var dataObj) && dataObj is IDictionary<string, object> data)
        {
            isLoadingFromNavigation = true;

            // Extract id
            if (data.TryGetValue("id", out var idObj) && idObj is string idString && Guid.TryParse(idString, out var guid))
            {
                Id = guid;
                CurrentMode = Mode.Edit;
                ActionButtonText = "Save Changes";
                IsDeleteButtonVisible = true;
            }

            // Extract company FIRST
            if (data.TryGetValue("company", out var companyObj) && companyObj is string companyString)
            {
                Company = companyString;
            }

            // Extract subscription AFTER company is set
            if (data.TryGetValue("subscription", out var subscriptionObj) && subscriptionObj is string subscriptionString)
            {
                SubscriptionPlan = subscriptionString;
            }

            // Load data từ API khi ở Edit mode
            if (CurrentMode == Mode.Edit && Id.HasValue)
            {
                var response = await subscriptionDetails.GetAsync(
                    new()
                    {
                        UserId = MyApp.CurrentUser.Id.ToString(),
                        Company = Company,
                        Subscription = SubscriptionPlan
                    },
                    Id.Value
                );

                // MAP DỮ LIỆU TỪ API VÀO CÁC PROPERTY
                if (response != null && response.Content != null)
                {
                    var detail = response.Content;

                    // Map Company
                    var matchingCompany = companies.FirstOrDefault(c =>
                        c.Name.Equals(detail.Company, StringComparison.OrdinalIgnoreCase));
                    if (matchingCompany != null)
                    {
                        SelectedCompany = matchingCompany;
                    }
                    CompanyValue = detail.Company;

                    // Load subscriptions cho company này
                    //FilteredSubscriptions = new ObservableCollection<Subscription>(matchingCompany.Subscriptions);

                    // Map Subscription
                    //var matchingSubscription = matchingCompany.Subscriptions.FirstOrDefault(s =>
                    //    s.Name.Equals(detail.Subscription, StringComparison.OrdinalIgnoreCase));
                    //if (matchingSubscription != null)
                    //{
                    //    SelectedSubscription = matchingSubscription;
                    SubscriptionValue = detail.Subscription;
                        //}
                    //}

                    // Map Price
                    Price = detail.Price;

                    // Map Color
                    SelectedColor = detail.Hex ?? "#FF6B6B";

                    // Map Next Renewal Date
                    NextRenewalSelectedDate = detail.RenewalDate;

                    // Map IsRecursive
                    IsRecursiveBill = detail.IsRecursive;
                }
            }

            isLoadingFromNavigation = false;
        }
    }

    #endregion

    #region [ Company ]

    [ObservableProperty]
    string section1Title = "Company";

    [ObservableProperty]
    string companySearchText = "Google, Apple, Microsoft";

    [ObservableProperty]
    string companyValue = string.Empty;

    private List<Provider> companies = new();
    //{
    //new () {
    //        Id = Guid.Parse("D3B2F1E4-5C6A-4B8D-9A2F-1B2C3D4E5F6A"),
    //        Name = "Google",
    //        Subscriptions = new List<Subscription>
    //        {
    //            new () { Id = Guid.NewGuid(), Name = "Google One" },
    //            new () { Id = Guid.NewGuid(), Name = "YouTube Premium" },
    //            new() { Id = Guid.NewGuid(), Name = "Google Workspace" },
    //        }
    //      },
    //new()
    //{
    //    Id = Guid.Parse("D3B2F1E4-5C6A-4B8D-9A2F-1B2C3D4E5F6B"),
    //    Name = "Apple",
    //    Subscriptions = new List<Subscription>
    //        {
    //            new() { Id = Guid.NewGuid(), Name = "Apple Music" },
    //            new() { Id = Guid.NewGuid(), Name = "Apple TV+" },
    //            new() { Id = Guid.NewGuid(), Name = "iCloud" },
    //        }
    //},
    //new()
    //{
    //    Id = Guid.Parse("D3B2F1E4-5C6A-4B8D-9A2F-1B2C3D4E5F6C"),
    //    Name = "Microsoft",
    //    Subscriptions = new List<Subscription>
    //        {
    //            new() { Id = Guid.NewGuid(), Name = "Microsoft 365" },
    //            new() { Id = Guid.NewGuid(), Name = "Xbox Game Pass" },
    //            new() { Id = Guid.NewGuid(), Name = "OneDrive" },
    //        }
    //    },
    //};

    [ObservableProperty]
    private ObservableCollection<Provider> filteredCompanies;

    [ObservableProperty]
    private Provider selectedCompany;

    partial void OnSelectedCompanyChanged(Provider value)
    {
        if (value != null)
        {
            CompanyValue = value.Name;
            FilteredSubscriptions = new(value.Subscriptions);

            // Only clear subscription if NOT loading from navigation (i.e., user manually selected)
            if (!isLoadingFromNavigation)
            {
                SubscriptionValue = string.Empty;
                SelectedSubscription = null!;
            }
        }
    }

    [RelayCommand]
    private void CompanyTextChanged(string text)
    {
        FilteredCompanies.Clear();
        var filtered = companies.Where(item =>
            item.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered)
            FilteredCompanies.Add(item);
    }
    #endregion

    #region [ Subscriptions ]

    [ObservableProperty]
    string section2Title = "Subscription Plan";

    [ObservableProperty]
    string subscriptionSearchText = "YouTube Premium, Spotify Family";

    [ObservableProperty]
    string subscriptionValue = string.Empty;

    //private readonly List<Subscription> subscriptions = new()
    //{
    //    new() { Id = Guid.NewGuid(), Name = "Individual" },
    //    new() { Id = Guid.NewGuid(), Name = "Family" },
    //    new() { Id = Guid.NewGuid(), Name = "Student plans" },
    //};

    [ObservableProperty]
    private ObservableCollection<Subscription> filteredSubscriptions;

    [ObservableProperty]
    private Subscription selectedSubscription;

    partial void OnSelectedSubscriptionChanged(Subscription value)
    {
        if (value != null)
        {
            SubscriptionValue = value.Name;
        }
    }

    [RelayCommand]
    private void SubscriptionTextChanged(string text)
    {
        FilteredSubscriptions.Clear();
        //var filtered = subscriptions.Where(item =>
        //    item.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
        //foreach (var item in filtered)
        //    FilteredSubscriptions.Add(item);
    }
    #endregion

    #region [ Colors ]

    [ObservableProperty]
    string section3Title = "Color";

    private readonly List<string> quickColors = new()
    {
        "#FF6B6B",
        "#4ECDC4",
        "#45B7D1",
        "#FFA07A",
        "#98D8C8",
        "#F7DC6F",
        "#BB8FCE",
        "#85C1E2",
        "#F8B88B",
        "#52C9D8",
        "#A8E6CF",
        "#FFD3B6",
        "#FFAAA5",
        "#FF8B94",
        "#A8D8EA",
        "#7FDBCA",
        "#FFB4B4",
        "#BAFFC9"
    };

    [ObservableProperty]
    private ObservableCollection<string> colors;

    [ObservableProperty]
    string selectedColor = "#FF6B6B";

    partial void OnSelectedColorChanged(string value)
    {

    }
    #endregion

    #region [ Price ]

    [ObservableProperty]
    string section4Title = "Price (VND)";

    [ObservableProperty]
    private decimal price;
    #endregion

    #region [ Next Renewable Date ]

    [ObservableProperty]
    string section5Title = "Next Renewal Date";

    [ObservableProperty]
    bool isRecursiveBill = false;

    [ObservableProperty]
    DateTime nextRenewalSelectedDate = DateTime.Now;
    #endregion

    #region [ Action Button ]

    [ObservableProperty]
    Mode currentMode = Mode.Add;
    [ObservableProperty]
    string actionButtonText = "Add";

    //partial void OnCurrentModeChanged(Mode value)
    //{
    //    switch (value)
    //    {
    //        case Mode.Add:
    //            ActionButtonText = "Create New Subscription";
    //            subscriptionDetails.CreateAsync(new()
    //            {
    //                UserId = MyApp.CurrentUser.Id.ToString(),
    //                Company = ,
    //                Subscription = ,
    //                Price = ,
    //                Currency = "VND",
    //                Hex = ,
    //                RenewalDate = ,
    //                IsRecursive =
    //            });
    //            break;
    //        case Mode.Edit:
    //            ActionButtonText = "Save Changes";
    //            break;
    //        default:
    //            ActionButtonText = "Create New Subscription";
    //            break;
    //    }
    //}


    [RelayCommand]
    private async Task ActionButtonCommand()
    {
        // Validation trước khi submit
        if (string.IsNullOrEmpty(CompanyValue))
        {
            await ShowSnackBarAsync("Please select a company");
            return;
        }

        if (string.IsNullOrEmpty(SubscriptionValue))
        {
            await ShowSnackBarAsync("Please select a subscription plan");
            return;
        }

        if (Price <= 0)
        {
            await ShowSnackBarAsync("Please enter a valid price");
            return;
        }

        switch (CurrentMode)
        {
            case Mode.Add:
                try
                {
                    var createResponse = await subscriptionDetails.CreateAsync(new()
                    {
                        UserId = MyApp.CurrentUser.Id.ToString(),
                        Company = CompanyValue,
                        Subscription = SubscriptionValue,
                        Price = Price,
                        Currency = "VND",
                        Hex = SelectedColor,
                        RenewalDate = NextRenewalSelectedDate,
                        IsRecursive = IsRecursiveBill
                    });

                    if (createResponse != null && createResponse.IsSuccessStatusCode)
                    {
                        await ShowSnackBarAsync("Created successfully!");
                        await AppNavigator.GoBackAsync();
                    }
                    else
                    {
                        await ShowSnackBarAsync("Failed to create subscription");
                    }
                }
                catch (Exception ex)
                {
                    await ShowSnackBarAsync($"Error: {ex.Message}");
                }
                break;

            case Mode.Edit:
                try
                {
                    if (!Id.HasValue)
                    {
                        await ShowSnackBarAsync("Invalid subscription ID");
                        return;
                    }

                    var updateResponse = await subscriptionDetails.SaveAsync(new()
                    {
                        UserId = MyApp.CurrentUser.Id.ToString(),
                        OldCompany = Company ?? string.Empty,  // Company ban đầu từ navigation
                        OldSubscription = SubscriptionPlan ?? string.Empty,  // Subscription ban đầu từ navigation
                        NewCompany = CompanyValue,  // Company mới từ UI
                        NewSubscription = SubscriptionValue,  // Subscription mới từ UI
                        Price = Price,
                        Currency = "VND",
                        Discount = null,  // Nếu có field Discount trong UI thì map vào
                        DiscountedPrice = null,  // Nếu có field DiscountedPrice trong UI thì map vào
                        Hex = SelectedColor,
                        RenewalDate = NextRenewalSelectedDate,
                        IsRecursive = IsRecursiveBill,
                        IsDiscountApplied = null,  // Nếu có field này trong UI thì map vào
                        IsDiscountAvailable = null  // Nếu có field này trong UI thì map vào
                    }, Id.Value);


                    if (updateResponse != null && updateResponse.IsSuccessStatusCode)
                    {
                        await ShowSnackBarAsync("Saved successfully!");
                        await AppNavigator.GoBackAsync();
                    }
                    else
                    {
                        await ShowSnackBarAsync("Failed to save changes");
                    }
                }
                catch (Exception ex)
                {
                    await ShowSnackBarAsync($"Error: {ex.Message}");
                }
                break;

            default:
                break;
        }
    }

    public async Task ShowSnackBarAsync(string message)
    {
        await AppNavigator.ShowSnackbarAsync(message);
    }
    #endregion

    #region [ Delete Button ]
    [ObservableProperty]
    bool isDeleteButtonVisible = false;

    [RelayCommand]
    private async Task DeleteButtonCommand()
    {
        if (!Id.HasValue)
        {
            await ShowSnackBarAsync("Invalid subscription ID");
            return;
        }
        try
        {
            var deleteResponse = await subscriptionDetails.DeleteAsync(
                new()
                {
                    UserId = MyApp.CurrentUser.Id.ToString(),
                    SubscriptionPlan = SubscriptionPlan ?? string.Empty,
                    CompanyName = Company ?? string.Empty
                },
                Id.Value
            );
            if (deleteResponse != null && deleteResponse.IsSuccessStatusCode)
            {
                await ShowSnackBarAsync("Deleted successfully!");
                await AppNavigator.GoBackAsync();
            }
            else
            {
                await ShowSnackBarAsync("Failed to delete subscription");
            }
        }
        catch (Exception ex)
        {
            await ShowSnackBarAsync($"Error: {ex.Message}");
        }
    }
    #endregion
}




public class Provider
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public class Subscription
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
}

public enum Mode
{
    Add,
    Edit
}