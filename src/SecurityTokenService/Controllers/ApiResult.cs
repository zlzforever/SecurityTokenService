// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SecurityTokenService.Controllers;

public class ApiResult
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; }
    public bool Success { get; set; } = true;
}