using System;

namespace SecurityTokenService;

public class FriendlyException : Exception
{
    public FriendlyException(string message) : base(message) { }
}
