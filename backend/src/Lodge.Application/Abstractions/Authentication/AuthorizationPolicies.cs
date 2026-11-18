namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// Authorization policy and claim names shared by the infrastructure (token issuing and validation)
/// and the presentation layer (endpoint authorization).
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires the <c>Admin</c> role.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// The access-token claim that carries the user's role (<c>"Guest"</c> or <c>"Admin"</c>).
    /// </summary>
    public const string RoleClaimType = "role";
}
