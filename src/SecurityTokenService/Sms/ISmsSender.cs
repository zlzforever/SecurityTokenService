using System.Threading.Tasks;

namespace SecurityTokenService.Sms;

public interface ISmsSender
{
    Task SendAsync(string number, string code);
}
