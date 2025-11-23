namespace BFF.Subscriptions;

public static class Extensions
{
    public static IServiceCollection AddSubcriptions(this IServiceCollection services)
    {
        services.AddScoped<All.IMapper, All.Mapper>();
        services.AddScoped<IMapper, Mapper>();
        return services;
    }
}
