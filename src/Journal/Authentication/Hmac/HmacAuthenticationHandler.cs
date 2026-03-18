using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Journal.Authentication.Hmac;

public class HmacAuthenticationHandler : AuthenticationHandler<HmacOptions>
{
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<HmacOptions> _options;

    public HmacAuthenticationHandler(
        IOptionsMonitor<HmacOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IMemoryCache cache)
        : base(options, logger, encoder, clock)
    {
        _cache = cache;
        _options = options;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;
        var timestamp = headers[_options.CurrentValue.HeaderTimestamp].FirstOrDefault();
        var machineHash = headers[_options.CurrentValue.HeaderMachineHash].FirstOrDefault();
        var nonce = headers[_options.CurrentValue.HeaderNonce].FirstOrDefault();
        var secretKey = _options.CurrentValue.SecretKey;

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(machineHash) || string.IsNullOrEmpty(nonce))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var nonceKey = $"hmac-nonce:{nonce}";
        if (_cache.TryGetValue(nonceKey, out _))
        {
            return Task.FromResult(AuthenticateResult.Fail("Replay attack detected."));
        }

        _cache.Set(nonceKey, true, Options.NonceLifetime);

        var tsClient = DateTime.ParseExact(timestamp, "yyyyMMddHHmmss", null);
        var tsServer = DateTime.Now; // hoặc DateTime.UtcNow nếu muốn chuẩn hóa

        var diffMinutes = Math.Abs((tsClient - tsServer).TotalMinutes);

        if (diffMinutes > 5 || diffMinutes < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Timestamp is too old or too far in the future."));
        }

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
