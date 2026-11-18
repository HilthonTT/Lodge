using System.Security.Claims;
using System.Text;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lodge.Infrastructure.Authentication;

/// <summary>
/// Issues the access tokens. The custom claim names (<c>userId</c>, <c>email</c>, <c>name</c>, <c>imageId</c>,
/// <c>role</c>, <c>emailVerified</c>) are part of the contract with the frontend. <c>role</c> is <c>"Guest"</c> or
/// <c>"Admin"</c>; <c>emailVerified</c> is <c>"true"</c> or <c>"false"</c>.
/// </summary>
internal sealed class JwtProvider(IDateTimeProvider dateTimeProvider, IOptions<JwtOptions> options) : IJwtProvider
{
    internal const string UserIdClaim = "userId";
    internal const string NameClaim = "name";
    internal const string RoleClaim = AuthorizationPolicies.RoleClaimType;
    internal const string EmailVerifiedClaim = AuthorizationPolicies.EmailVerifiedClaimType;
    internal const string PurposeClaim = "purpose";
    internal const string TwoFactorPurpose = "two-factor";

    internal static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    private static readonly JsonWebTokenHandler TokenHandler = new();

    public string Create(User user)
    {
        JwtOptions jwtOptions = options.Value;

        DateTime utcNow = dateTimeProvider.UtcNow;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = utcNow.AddMinutes(jwtOptions.TokenExpirationInMinutes),
            SigningCredentials = CreateSigningCredentials(jwtOptions),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(UserIdClaim, user.Id.ToString()),
                new Claim("email", user.Email.Value),
                new Claim(NameClaim, user.FullName),
                new Claim("imageId", user.ImageId?.ToString() ?? string.Empty),
                new Claim(RoleClaim, user.Role.ToString()),
                new Claim(EmailVerifiedClaim, user.IsEmailVerified ? "true" : "false"),
            ]),
        };

        return TokenHandler.CreateToken(tokenDescriptor);
    }

    public string CreateTwoFactorChallenge(User user)
    {
        JwtOptions jwtOptions = options.Value;

        DateTime utcNow = dateTimeProvider.UtcNow;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = TwoFactorAudience(jwtOptions),
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = utcNow.Add(TwoFactorChallengeLifetime),
            SigningCredentials = CreateSigningCredentials(jwtOptions),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(PurposeClaim, TwoFactorPurpose),
            ]),
        };

        return TokenHandler.CreateToken(tokenDescriptor);
    }

    public async Task<TwoFactorChallenge?> ValidateTwoFactorChallengeAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        JwtOptions jwtOptions = options.Value;

        TokenValidationResult result = await TokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = TwoFactorAudience(jwtOptions),
            ValidateLifetime = true,
            LifetimeValidator = (notBefore, expires, _, _) =>
                expires is not null && dateTimeProvider.UtcNow < expires.Value &&
                (notBefore is null || dateTimeProvider.UtcNow >= notBefore.Value.AddSeconds(-30)),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        });

        if (!result.IsValid ||
            !result.Claims.TryGetValue(PurposeClaim, out object? purpose) || purpose as string != TwoFactorPurpose ||
            !result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out object? subject) ||
            !Guid.TryParse(subject as string, out Guid userId) ||
            !result.Claims.TryGetValue(JwtRegisteredClaimNames.Jti, out object? tokenId) ||
            tokenId is not string tokenIdValue)
        {
            return null;
        }

        return new TwoFactorChallenge(userId, tokenIdValue);
    }

    // A separate audience, so a challenge token is never accepted as an access token (and vice versa).
    private static string TwoFactorAudience(JwtOptions jwtOptions) => $"{jwtOptions.Audience}/two-factor";

    private static SigningCredentials CreateSigningCredentials(JwtOptions jwtOptions) =>
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)), SecurityAlgorithms.HmacSha256);
}
