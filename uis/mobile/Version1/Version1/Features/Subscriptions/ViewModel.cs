using CommunityToolkit.Maui.Core.Extensions;
using Navigation;
using Version1.Features.Subscriptions;

public partial class ViewModel : NavigationAwareBaseViewModel
{
    private readonly Core.Subscriptions.IRefitInterface _subscriptions;

    [ObservableProperty]
    ObservableCollection<AppUsage> appUsages;

    [ObservableProperty]
    ObservableCollection<ChartSlice> chartSlices;

    [ObservableProperty]
    ObservableCollection<Brush> customBrushes;

    [ObservableProperty]
    string month;

    [ObservableProperty]
    decimal totalPrice;

    [ObservableProperty]
    bool isLoading;

    [ObservableProperty]
    bool hasData;

    public ViewModel(IAppNavigator appNavigator, Core.Subscriptions.IRefitInterface subscriptions)
        : base(appNavigator)
    {
        _subscriptions = subscriptions;
        AppUsages = new ObservableCollection<AppUsage>();
        Month = string.Empty;
        TotalPrice = 0;
        ChartSlices = new ObservableCollection<ChartSlice>();
        CustomBrushes = new ObservableCollection<Brush>();
        IsLoading = false;
        HasData = false;
    }

    public async Task LoadSubscriptionsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        HasData = false;

        try
        {
            var result = await _subscriptions.AllAsync(new() { UserId = MyApp?.CurrentUser?.Id.ToString() });

            AppUsages.Clear();
            ChartSlices.Clear();
            CustomBrushes.Clear();

            if (result?.Content?.AppUsages != null)
            {
                foreach (var appUsage in result.Content.AppUsages)
                {
                    AppUsages.Add(new AppUsage
                    {
                        Id = appUsage.Id.ToString(),
                        Company = appUsage.Company,
                        Icon = appUsage.Icon,
                        Subscription = appUsage.Subscription,
                        Price = appUsage.Price,
                        Hex = appUsage.Hex ?? "#FF6B6B",
                        DayLeft = appUsage.DayLeft,
                        IsPaid = appUsage.IsPaid,
                    });
                }
            }

            if (result?.Content?.ChartSlices != null)
            {
                foreach (var chartSlice in result.Content.ChartSlices)
                {
                    ChartSlices.Add(new ChartSlice
                    {
                        Subscription = chartSlice.Subscription,
                        Price = chartSlice.Price,
                    });
                }
            }

            if (result?.Content?.CustomBrushes != null)
            {
                foreach (var customBrush in result.Content.CustomBrushes)
                {
                    CustomBrushes.Add(new SolidColorBrush(Color.FromArgb(customBrush)));
                }
            }

            Month = result?.Content?.Month ?? string.Empty;
            TotalPrice = result?.Content?.TotalPrice ?? 0;

            HasData = AppUsages.Any();
        }
        catch (Exception ex)
        {
            await ShowSnackBarAsync($"Error loading subscriptions: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ShowSnackBarAsync(string message)
    {
        await AppNavigator.ShowSnackbarAsync(message);
    }

    public async Task NavigateAsync(string route, object args)
        => await AppNavigator.NavigateAsync(route, args: args, animated: true);
}