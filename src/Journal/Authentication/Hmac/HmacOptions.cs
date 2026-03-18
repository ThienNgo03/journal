using Microsoft.AspNetCore.Authentication;

namespace Journal.Authentication.Hmac;

public class HmacOptions : AuthenticationSchemeOptions
{
    public string HeaderTimestamp { get; set; } = "X-Timestamp";
    public string HeaderNonce { get; set; } = "X-Nonce";
    public string HeaderMachineHash { get; set; } = "X-Machine-Hash";
    public TimeSpan NonceLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public string? SecretKey { get; set; } = string.Empty;
}

