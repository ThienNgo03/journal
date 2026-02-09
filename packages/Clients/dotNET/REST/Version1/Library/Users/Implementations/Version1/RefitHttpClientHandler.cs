namespace Library.Users.Implementations.Version1;
public class RefitHttpClientHandler : HttpClientHandler
{
    private readonly Config config;
    private readonly Token.Service tokenService;
    private readonly MachineToken.Service machineTokenService;
    public RefitHttpClientHandler(Config config,
                                  Token.Service tokenService,
                                  MachineToken.Service machineTokenService)
    {
        this.config = config;
        this.tokenService = tokenService;
        this.machineTokenService = machineTokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Nếu có SecretKey thì thêm HMAC headers
        //if (!string.IsNullOrEmpty(config.SecretKey) && !string.IsNullOrWhiteSpace(config.SecretKey))
        //{
        //    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        //    string nonce = Guid.NewGuid().ToString("N").Substring(0, 8);
        //    request.Headers.Add("X-Timestamp", timestamp);
        //    request.Headers.Add("X-Nonce", nonce);
        //    request.Headers.Add("X-Machine-Hash", machineTokenService.ComputeHash(timestamp, nonce));
        //}

        // Nếu có JWT thì thêm Authorization header
        var jwt = tokenService.GetToken();
        if (!string.IsNullOrEmpty(jwt))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

