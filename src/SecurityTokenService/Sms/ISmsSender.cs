using System.Threading.Tasks;

namespace SecurityTokenService.Sms;

/// <summary>
/// short message service
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string number, string code);
}
