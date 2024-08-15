// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SecurityTokenService.Identity;

public sealed class IdentityExtensionOptions
{
    /// <summary>
    /// 软删除的列名
    /// </summary>
    public string SoftDeleteColumn { get; set; }

    /// <summary>
    /// 表前缀
    /// </summary>
    public string TablePrefix { get; set; }
    
    /// <summary>
    /// 开启失败锁定功能
    /// </summary>
    public bool LockoutOnFailureOff { get; set; }
}