using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

public class HmacAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public HmacAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration,
        IMemoryCache cache)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
        _cache = cache;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;
        var timestamp = headers["X-Timestamp"].FirstOrDefault();
        var machineHash = headers["X-Machine-Hash"].FirstOrDefault();
        var nonce = headers["X-Nonce"].FirstOrDefault();
        var secretKey = _configuration["MachineAuth:SecretKey"];

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(machineHash) || string.IsNullOrEmpty(nonce))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var nonceKey = $"hmac-nonce:{nonce}";
        if (_cache.TryGetValue(nonceKey, out _))
        {
            return Task.FromResult(AuthenticateResult.Fail("Replay attack detected."));
        }

        _cache.Set(nonceKey, true, TimeSpan.FromMinutes(5));

        var message = timestamp + nonce;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var computedHash = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).Replace("-", "").ToLower();

        if (computedHash != machineHash?.ToLower())
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid HMAC."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "BFF"),
            new Claim(ClaimTypes.Role, "Machine"),
            new Claim("AuthType", "HMAC")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}