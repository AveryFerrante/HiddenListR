using HiddenListR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace HiddenListR.Auth;

public sealed class AdminOnlyAttribute() : AuthorizeAttribute(nameof(AdminOnlyAttribute))
{
    public const string PolicyName = nameof(AdminOnlyAttribute);

    public static void ConfigurePolicy(AuthorizationPolicyBuilder policy) =>
        policy.RequireRole(nameof(UserRole.Admin));
}
