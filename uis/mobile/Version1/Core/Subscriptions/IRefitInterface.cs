using Refit;
namespace Core.Subscriptions;

public interface IRefitInterface
{
    [Get("/api/subscriptions/all")]
    Task<ApiResponse<Model>> AllAsync([Query] All.Parameters parameters);

}
