using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Subscriptions.Details;

public class RefitHttpClientHandler : HttpClientHandler
{
    private readonly Token.Service tokenService;
    public RefitHttpClientHandler(Token.Service tokenService)
    {
        this.tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenService.GetToken());
        return await base.SendAsync(request, cancellationToken);
    }
}
