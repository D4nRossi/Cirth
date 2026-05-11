namespace Cirth.Application.Common.Ports;

public interface IApiKeyHasher
{
    string Hash(string plainKey);
    bool Verify(string plainKey, string hash);
    string Generate();
}
