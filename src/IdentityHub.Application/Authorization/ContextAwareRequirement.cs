using Microsoft.AspNetCore.Authorization;

namespace IdentityHub.Application.Authorization;

/// <summary>
/// Requirement for context-aware policy evaluation
/// </summary>
public class ContextAwareRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; }

    public ContextAwareRequirement(string policyName)
    {
        PolicyName = policyName;
    }
}
