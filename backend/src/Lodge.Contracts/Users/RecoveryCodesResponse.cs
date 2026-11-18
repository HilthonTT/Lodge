namespace Lodge.Contracts.Users;

/// <summary>
/// Freshly generated single-use recovery codes. They are only shown once.
/// </summary>
/// <param name="RecoveryCodes">The ten recovery codes, formatted as <c>xxxxx-xxxxx</c>.</param>
public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
