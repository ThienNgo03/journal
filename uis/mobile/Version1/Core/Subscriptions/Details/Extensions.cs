using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Core.Subscriptions.Details;

public static class Extensions
{
    public static void RegisterSubscriptionDetails(this IServiceCollection services, Config config)
    {
        services.AddTransient<RefitHttpClientHandler>();

        string baseUrl = config.Url;

        services.AddRefitClient<IRefitInterface>()
                .ConfigurePrimaryHttpMessageHandler<RefitHttpClientHandler>()
                .ConfigureHttpClient(x => x.BaseAddress = new Uri(baseUrl));
    }
}
