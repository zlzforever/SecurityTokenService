using System.Collections.Generic;

namespace SecurityTokenService.IdentityServer;

public class AliyunSMSOptions
{
    public string SignName { get; set; }
    public Dictionary<string, string> Templates { get; set; }
}