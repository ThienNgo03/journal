using System.Security.Cryptography;
using System.Text;

namespace Library.MachineToken;

public class Service
{
    private string secretKey;

    public Service(Config config)
    {
        this.secretKey = config.SecretKey;
    }

    public string ComputeHash(string timestamp, string nonce)
    {
        string message = timestamp + nonce;
        return ComputeHmacSha256(message, secretKey);
    }

    static string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

}
