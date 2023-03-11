using System.Threading.Tasks;

namespace SecurityTokenService.Stores;

public interface IPhoneCodeStore
{
    Task InitializeAsync();

    /// <summary>
    /// 获取指定时间内的验证码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="ttl"></param>
    /// <returns></returns>
    Task<string> GetAsync(string phoneNumber, int ttl = 300);

    /// <summary>
    /// 更新验证码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    Task UpdateAsync(string phoneNumber, string code);
}
