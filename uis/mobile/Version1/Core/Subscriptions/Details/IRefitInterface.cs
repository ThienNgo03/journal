using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Subscriptions.Details;

public interface IRefitInterface
{
    [Get("/api/subscriptions/{id}/detail")]
    Task<ApiResponse<Get.Response>> GetAsync([Query] Get.Parameters parameters, [AliasAs("id")] Guid id);

    [Post("/api/subscriptions/create")]
    Task<ApiResponse<object>> CreateAsync([Body] Create.Payload payload);

    [Put("/api/subscriptions/{id}/save")]
    Task<ApiResponse<object>> SaveAsync([Body] Save.Payload payload, [AliasAs("id")] Guid id);

    [Delete("/api/subscriptions/{id}/delete")]
    Task<ApiResponse<object>> DeleteAsync([Query] Delete.Parameters parameters, [AliasAs("id")] Guid id);

    [Get("/api/subscriptions/all-companies")]
    Task<ApiResponse<List<AllCompanies.Response>>> AllCompaniesAsync();
}
