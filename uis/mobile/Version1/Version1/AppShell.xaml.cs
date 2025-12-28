using Version1.UI;

namespace Version1;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();


        Routing.RegisterRoute("sign-up", typeof(Features.Authentication.SignUp.Page));
        Routing.RegisterRoute("forgot-password", typeof(Features.Authentication.ForgotPassword.Page));
        Routing.RegisterRoute("exercise-detail", typeof(Features.Exercises.Detail.Page));
        Routing.RegisterRoute("exercise-config", typeof(Features.Exercises.Config.Page));
        Routing.RegisterRoute("subscription-detail", typeof(Features.Subscriptions.Detail.Page));
    }

    async Task RequestImagesAndFilesPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Unable to get camera permission.", "OK");
            return;
        }

        status = await Permissions.CheckStatusAsync<Permissions.Photos>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Photos>();
        }
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Unable to get photos permission.", "OK");
            return;
        }

        status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.StorageRead>();
        }
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Unable to get storage read permission.", "OK");
            return;
        }
    }


    private void Shell_Loaded(object sender, EventArgs e)
    {
        RequestImagesAndFilesPermission().FireAndForget();
    }
}
