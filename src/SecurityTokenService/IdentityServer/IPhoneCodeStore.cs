using System.Threading.Tasks;

namespace SecurityTokenService.IdentityServer;

public interface IPhoneCodeStore
{
    Task InitializeAsync();

    Task<string> GetAsync(string phoneNumber, int ttl = 300);

    Task UpdateAsync(string phoneNumber, string code);
}