namespace Lodge.Domain.Users;

/// <summary>
/// Represents the role of a user. Persisted and issued in the access token by name (<c>"Guest"</c>, <c>"Admin"</c>).
/// </summary>
public enum UserRole
{
    Guest = 0,
    Admin = 1,
}
