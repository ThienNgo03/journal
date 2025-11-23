using Syncfusion.Maui.Toolkit.Buttons;

namespace Version1.Features.Subscriptions;

public partial class Page : ContentPage
{
    #region [ Fields ]

    private readonly ViewModel viewModel;
    #endregion

    #region [ CTors ]

    public Page(ViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = this.viewModel = viewModel;
    }
    #endregion

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Navigate to the details page
        var appUsage = e.CurrentSelection.FirstOrDefault() as AppUsage;
        if (appUsage != null)
        {
            Navigation.PushAsync(new Version1.Features.Subscriptions.Detail.Page());

        }
    }
}

