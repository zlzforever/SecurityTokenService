using System.Collections.Generic;

namespace SecurityTokenService.Options;

public class AliYunSMSOptions
{
    public string SignName { get; set; }
    public Dictionary<string, string> Templates { get; set; }
}
