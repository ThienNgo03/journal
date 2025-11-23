
using Mvvm;
using Navigation;

namespace Version1.Features.Subscriptions;

public partial class ViewModel(
    IAppNavigator appNavigator,
    Core.Exercises.Interface exercises ) : BaseViewModel(appNavigator)
{
}


public partial class AppUsage : ObservableObject
{
    [ObservableProperty]
    string company;

    [ObservableProperty]
    string icon;

    [ObservableProperty]
    string subscription;

    [ObservableProperty]
    double usagePercent;

    [ObservableProperty]
    decimal price;

    [ObservableProperty]
    string? discount; // "$10.99 (-15$) format

    [ObservableProperty]
    string hex;

    [ObservableProperty]
    string dayLeft;

    [ObservableProperty]
    bool isPaid;

    [ObservableProperty]
    bool isDiscountApplied;
}

public class SumPriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var items = value as IEnumerable<AppUsage>;
        return items?.Sum(i => i.Price) ?? 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TextDecorationsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

public class USDToVNDConverter : IValueConverter
{
    // Tỷ giá USD sang VND (có thể điều chỉnh theo tỷ giá hiện tại)
    private const decimal ExchangeRate = 24500m;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal usdPrice)
        {
            decimal vndPrice = usdPrice * ExchangeRate;
            return vndPrice.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
        }

        if (value is IEnumerable<AppUsage> items)
        {
            decimal totalUSD = items.Sum(i => i.Price);
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
