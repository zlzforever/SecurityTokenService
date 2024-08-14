using IdentityServer4.Validation;

namespace SecurityTokenServicePluginDemo;

/// <summary>
/// 总是允许校验不通过
/// </summary>
public class DisableAnyOneValidator : IExtensionGrantValidator
{
    public string GrantType => "disableAnyOne";

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        throw new Exception("DisableAnyOne!");
    }
}
