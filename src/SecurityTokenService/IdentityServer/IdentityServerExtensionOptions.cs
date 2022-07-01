// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SecurityTokenService.IdentityServer
{
    public sealed class IdentityServerExtensionOptions
    {
        public string TablePrefix { get; set; }
        
        /// <summary>
        /// 若 ID4 在反代的后端，反代和 ID4 实例变成了 HTTP 请求，会导致 ID4 组件返回的所有配置都是 HTTP 的，
        /// 使用此配置强制让 ID4 认为是 HTTP
        /// 使用此中间件后，ID4 只能以 HTTPS 的模式工作
        /// </summary>
        public bool EnableHttps { get; set; }
    }
}