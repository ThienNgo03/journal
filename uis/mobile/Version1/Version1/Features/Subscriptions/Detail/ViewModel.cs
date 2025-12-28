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

    [ObservableProperty]
    bool isBusy = false;

    public async Task ApplyQueryAttributes(IDictionary<string, object> query)
    {
        IsBusy = true;

        try
        {
            // Load companies từ API
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

            // Gán FilteredCompanies NGAY để nó có data
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

                // Extract company
                if (data.TryGetValue("company", out var companyObj) && companyObj is string companyString)
                {
                    Company = companyString;
                }

                // Extract subscription
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

                    if (response != null && response.Content != null)
                    {
                        var detail = response.Content;

                        // Tìm matching company
                        var matchingCompany = companies.FirstOrDefault(c =>
                            c.Name.Equals(detail.Company, StringComparison.OrdinalIgnoreCase));

                        // Update properties TRƯỚC KHI tắt IsBusy
                        CompanyValue = detail.Company;
                        SubscriptionValue = detail.Subscription;
                        Price = detail.Price;
                        SelectedColor = detail.Hex ?? "#FF6B6B";
                        NextRenewalSelectedDate = detail.RenewalDate;
                        IsRecursiveBill = detail.IsRecursive;

                        // Set SelectedCompany SAU CÙNG vì nó trigger cascade updates
                        if (matchingCompany != null)
                        {
                            SelectedCompany = matchingCompany;
                        }
                    }
                }

                isLoadingFromNavigation = false;
            }
        }
        finally
        {
            // Tắt loading
            IsBusy = false;

            // Yield UI thread để UI có thể render và respond ngay
            await Task.Yield();
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

    [ObservableProperty]
    private ObservableCollection<Provider> filteredCompanies;

    [ObservableProperty]
    private Provider selectedCompany;

    partial void OnSelectedCompanyChanged(Provider value)
    {
        if (value != null)
        {
            CompanyValue = value.Name;

            // Update subscriptions dựa trên company được chọn
            var subscriptions = value.Subscriptions?.ToList() ?? new List<Subscription>();
            FilteredSubscriptions = new ObservableCollection<Subscription>(subscriptions);

            // Only clear subscription if NOT loading from navigation
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
        if (string.IsNullOrWhiteSpace(text))
        {
            FilteredCompanies = new ObservableCollection<Provider>(companies);
            return;
        }

        var filtered = companies
            .Where(item => item.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredCompanies = new ObservableCollection<Provider>(filtered);
    }
    #endregion

    #region [ Subscriptions ]

    [ObservableProperty]
    string section2Title = "Subscription Plan";

    [ObservableProperty]
    string subscriptionSearchText = "YouTube Premium, Spotify Family";

    [ObservableProperty]
    string subscriptionValue = string.Empty;

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
        if (SelectedCompany?.Subscriptions == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            FilteredSubscriptions = new ObservableCollection<Subscription>(SelectedCompany.Subscriptions);
            return;
        }

        var filtered = SelectedCompany.Subscriptions
            .Where(item => item.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredSubscriptions = new ObservableCollection<Subscription>(filtered);
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

        IsBusy = true;

        try
        {
            switch (CurrentMode)
            {
                case Mode.Add:
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
                    break;

                case Mode.Edit:
                    if (!Id.HasValue)
                    {
                        await ShowSnackBarAsync("Invalid subscription ID");
                        return;
                    }

                    var updateResponse = await subscriptionDetails.SaveAsync(new()
                    {
                        UserId = MyApp.CurrentUser.Id.ToString(),
                        OldCompany = Company ?? string.Empty,
                        OldSubscription = SubscriptionPlan ?? string.Empty,
                        NewCompany = CompanyValue,
                        NewSubscription = SubscriptionValue,
                        Price = Price,
                        Currency = "VND",
                        Discount = null,
                        DiscountedPrice = null,
                        Hex = SelectedColor,
                        RenewalDate = NextRenewalSelectedDate,
                        IsRecursive = IsRecursiveBill,
                        IsDiscountApplied = null,
                        IsDiscountAvailable = null
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
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            await ShowSnackBarAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
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

        IsBusy = true;

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
        finally
        {
            IsBusy = false;
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