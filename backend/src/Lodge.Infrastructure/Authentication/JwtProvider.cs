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
/// <c>role</c>) are part of the contract with the frontend. <c>role</c> is <c>"Guest"</c> or <c>"Admin"</c>.
/// </summary>
internal sealed class JwtProvider(IDateTimeProvider dateTimeProvider, IOptions<JwtOptions> options) : IJwtProvider
{
    internal const string UserIdClaim = "userId";
    internal const string NameClaim = "name";
    internal const string RoleClaim = AuthorizationPolicies.RoleClaimType;

    private static readonly JsonWebTokenHandler TokenHandler = new();

    public string Create(User user)
    {
        JwtOptions jwtOptions = options.Value;

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
            SecurityAlgorithms.HmacSha256);

        DateTime utcNow = dateTimeProvider.UtcNow;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = utcNow.AddMinutes(jwtOptions.TokenExpirationInMinutes),
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(UserIdClaim, user.Id.ToString()),
                new Claim("email", user.Email.Value),
                new Claim(NameClaim, user.FullName),
                new Claim("imageId", user.ImageId?.ToString() ?? string.Empty),
                new Claim(RoleClaim, user.Role.ToString()),
            ]),
        };

        return TokenHandler.CreateToken(tokenDescriptor);
    }
}
