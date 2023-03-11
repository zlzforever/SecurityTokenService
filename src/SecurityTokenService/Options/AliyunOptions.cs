namespace SecurityTokenService.Options;

public class AliyunOptions
{
    public string AccessKey { get; set; }
    public string Secret { get; set; }
    public string Endpoint { get; set; }
    public AliyunSMSOptions Sms { get; set; }
}
