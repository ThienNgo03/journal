namespace Version1.Features.Subscriptions.Detail;

public partial class Page : ContentPage, IQueryAttributable
{
    private readonly ViewModel _viewmodel;

    public Page(ViewModel viewmodel)
    {
        InitializeComponent();
        BindingContext = _viewmodel = viewmodel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {

        if (_viewmodel is not null)
        {
            _viewmodel.ApplyQueryAttributes(query);
        }
    }
}