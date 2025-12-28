using Syncfusion.Maui.Toolkit.Buttons;
using System.Threading.Tasks;

namespace Version1.Features.Subscriptions;

public partial class Page : ContentPage
{
    private readonly ViewModel viewModel;

    public Page(ViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    private async void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Navigate to the details page
        var appUsage = e.CurrentSelection.FirstOrDefault() as AppUsage;
        if (appUsage != null)
        {
            await viewModel.NavigateAsync(AppRoutes.SubscriptionDetail, new Dictionary<string, object>
            {
                { "id", appUsage.Id },
                { "company", appUsage.Company },
                { "subscription", appUsage.Subscription }
            });
        }
    }

    private async void ContentPage_Appearing(object sender, EventArgs e)
    {
        await viewModel.LoadSubscriptionsAsync();
    }

    private async void AddButton_Clicked(object sender, EventArgs e)
    {
        await viewModel.NavigateAsync(AppRoutes.SubscriptionDetail, new Dictionary<string, object>());
    }
}

public partial class AppUsage : ObservableObject
{
    [ObservableProperty]
    string? id;

    [ObservableProperty]
    string? company;

    [ObservableProperty]
    string? icon;

    [ObservableProperty]
    string? subscription;

    [ObservableProperty]
    double usagePercent;

    [ObservableProperty]
    decimal price;

    [ObservableProperty]
    string? discount;

    [ObservableProperty]
    string? hex;

    [ObservableProperty]
    string? dayLeft;

    [ObservableProperty]
    bool isPaid;

    [ObservableProperty]
    bool isDiscountApplied;

    [ObservableProperty]
    bool isDiscountAvailable;
}

public partial class ChartSlice: ObservableObject
{

    [ObservableProperty]
    string subscription;

    [ObservableProperty]
    decimal price;
}

public class TextDecorationsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDiscountApplied && isDiscountApplied)
        {
            return TextDecorations.Strikethrough;
        }
        return TextDecorations.None;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SumPriceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "Total: $0.00";

        if (value is IEnumerable<AppUsage> items)
        {
            var itemsList = items.ToList();
            if (!itemsList.Any()) return "Total: $0.00";

            decimal total = itemsList.Sum(i => i.Price);
            return $"Total: ${total:0.00}";
        }

        return "Total: $0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class USDToVNDConverter : IValueConverter
{
    private const decimal ExchangeRate = 24500m;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0 ₫";

        // Trường hợp convert 1 giá trị price
        if (value is decimal usdPrice)
        {
            decimal vndPrice = usdPrice * ExchangeRate;
            return vndPrice.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
        }

        // Trường hợp convert collection
        if (value is IEnumerable<AppUsage> items)
        {
            var itemsList = items.ToList();
            if (!itemsList.Any()) return "0 ₫";

            decimal totalUSD = itemsList.Sum(i => i.Price);
            decimal totalVND = totalUSD * ExchangeRate;
            return totalVND.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
        }

        return "0 ₫";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}