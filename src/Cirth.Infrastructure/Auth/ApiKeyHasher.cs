using Cirth.Application.Common.Ports;
using System.Security.Cryptography;

namespace Cirth.Infrastructure.Auth;

internal sealed class ApiKeyHasher : IApiKeyHasher
{
    private const int KeyLength = 32; // 256 bits

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyLength);
        return $"cirth_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
    }

    public string Hash(string plainKey)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plainKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string plainKey, string hash)
        => Hash(plainKey) == hash;
}
