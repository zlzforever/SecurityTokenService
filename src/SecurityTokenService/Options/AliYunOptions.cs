namespace SecurityTokenService.Options;

public class AliYunOptions
{
    public string AccessKey { get; set; }
    public string Secret { get; set; }
    public string Endpoint { get; set; }
    public AliYunSMSOptions Sms { get; set; }
}
