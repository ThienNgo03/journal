using UI;
using Mvvm;
using Core;
using Library;
using Navigation;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Version1.Features.Authentication;
using SkiaSharp.Views.Maui.Controls.Hosting;
using zoft.MauiExtensions.Controls;

namespace Version1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {       
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureSyncfusionToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Damageplan.ttf", "Damageplan");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FontNames.FluentSystemIconsRegular);
            })
            .RegisterCore()
            .RegisterFeatures()
            .RegisterPages()
            .UseZoftAutoCompleteEntry()
            .UseSkiaSharp();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    static MauiAppBuilder RegisterCore(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAppInfo>(AppInfo.Current);
        builder.Services.AddSingleton<IPreferences>(Preferences.Default);
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

        builder.Services.AddSingleton<IAppNavigator, AppNavigator>();



        var libraryConfig = new Library.Config(
                    url: "https://1bw60b9k-7011.asse.devtunnels.ms/",
                    secretKey: "secretKey"
                );
        builder.Services.AddEndpoints(libraryConfig);

        Core.Config config = new("https://1bw60b9k-7245.asse.devtunnels.ms/");
        builder.Services.AddBff(config);
        return builder;
    }

    static MauiAppBuilder RegisterFeatures(this MauiAppBuilder builder)
    {
        builder.RegisterAuthentication();
        return builder;
    }

    static MauiAppBuilder RegisterPages(this MauiAppBuilder builder)
    {
        var pageTypes = typeof(MauiProgram).Assembly
                            .GetTypes()
                            .Where(x => !x.IsAbstract &&
                                    x != typeof(BasePage) &&
                                    x.IsAssignableTo(typeof(BasePage)));
        foreach (var pageType in pageTypes)
        {
            builder.Services.AddTransient(pageType);
        }

        var viewModelTypes = typeof(MauiProgram).Assembly
                            .GetTypes()
                            .Where(
                                x => !x.IsAbstract &&
                                    x != typeof(BaseViewModel) &&
                                    x != typeof(NavigationAwareBaseViewModel) &&
                                    (x.IsAssignableTo(typeof(BaseViewModel)) ||
                                     x.IsAssignableTo(typeof(NavigationAwareBaseViewModel)))
                            )
                            .ToList();
        foreach (var vmType in viewModelTypes)
        {
            builder.Services.AddTransient(vmType);
        }

        return builder;
    }

    static IServiceCollection AddPopup<TPopup, TViewModel>(this IServiceCollection services, string name)
        where TPopup : BasePopup where TViewModel : BaseViewModel
    {
        Routing.RegisterRoute(name, typeof(TPopup));
        return services;
    }
}
