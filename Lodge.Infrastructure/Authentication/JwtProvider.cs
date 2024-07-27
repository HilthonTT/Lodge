using Lodge.Application.Abstractions.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Authentication.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Lodge.Infrastructure.Authentication;

/// <summary>
/// Represents the JWT provider.
/// </summary>
/// <param name="dateTimeProvider">The current date and time.</param>
/// <param name="options">The JWT options.</param>
internal sealed class JwtProvider(IDateTimeProvider dateTimeProvider, IOptions<JwtSettings> options) 
    : IJwtProvider
{
    private readonly JwtSettings _jwtSettings = options.Value;

    /// <inheritdoc />
    public string Create(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));

        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new("userId", user.Id.ToString()),
            new ("email", user.Email),
            new("name", user.FullName),
            new("imageId", user.ImageId.ToString() ?? "")
        ];

        DateTime tokenExpirationTime = dateTimeProvider.UtcNow.AddMinutes(_jwtSettings.TokenExpirationInMinutes);

        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            null,
            tokenExpirationTime,
            signingCredentials);

        string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return tokenValue;
    }
}
