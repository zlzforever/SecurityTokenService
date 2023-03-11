// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SecurityTokenService.Identity
{
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
    }
}
