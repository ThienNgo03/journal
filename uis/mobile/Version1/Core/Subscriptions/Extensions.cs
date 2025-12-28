using Microsoft.Extensions.DependencyInjection;
using Refit;
namespace Core.Subscriptions;

public static class Extensions
{
    public static void RegisterSubscriptions(this IServiceCollection services, Config config)
    {
        services.AddTransient<RefitHttpClientHandler>();

        string baseUrl = config.Url;

        services.AddRefitClient<IRefitInterface>()
                .ConfigurePrimaryHttpMessageHandler<RefitHttpClientHandler>()
                .ConfigureHttpClient(x => x.BaseAddress = new Uri(baseUrl));
    }
}
