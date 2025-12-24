namespace SecurityTokenService.Controllers;

public class RedirectResult(string location)
{
    public int Code => 302;
    public string Location { get; private set; } = location;
}
