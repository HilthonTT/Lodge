using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lodge.Infrastructure.Authentication;

/// <summary>
/// Configures JWT bearer validation from the validated <see cref="JwtOptions"/>.
/// </summary>
internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public const string HubsPath = "/hubs";

    public const string HubAccessTokenQueryKey = "access_token";

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        JwtOptions jwt = jwtOptions.Value;

        // Browsers cannot send headers with WebSocket (and Server-Sent Events) requests, so the SignalR hubs accept the
        // access token in the "access_token" query string. Only under /hubs: everywhere else the header is required.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query[HubAccessTokenQueryKey];

                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(HubsPath))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };

        // Keep the raw claim names ("userId", "email", "role", ...) instead of mapping them to WS-* URIs.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecurityKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtProvider.NameClaim,
            RoleClaimType = JwtProvider.RoleClaim,
        };
    }
}
