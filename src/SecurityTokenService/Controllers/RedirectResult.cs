namespace SecurityTokenService.Controllers;

public class RedirectResult
{
    public int Code => 302;
    public string Location { get; private set; }

    public RedirectResult(string location)
    {
        Location = location;
    }
}
