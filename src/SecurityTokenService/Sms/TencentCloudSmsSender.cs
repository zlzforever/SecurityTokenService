using System.Threading.Tasks;

namespace SecurityTokenService.Sms;

public class TencentCloudSmsSender : ISmsSender
{
    public Task SendAsync(string number, string code)
    {
        throw new System.NotImplementedException();
    }
}
