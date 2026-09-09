using System.Security.Claims;
using System.Text.Encodings.Web;
using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EnterpriseAgenticRag.Api;

public sealed class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<DevelopmentAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<DevelopmentAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, DevelopmentDefaults.UserId.ToString()),
            new("oid", DevelopmentDefaults.UserId.ToString()),
            new("tid", DevelopmentDefaults.TenantId.ToString()),
            new(ClaimTypes.Name, "Development User"),
            new("groups", "enterprise-employees")
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}

public sealed class HttpCurrentRequestIdentity(IHttpContextAccessor accessor) : ICurrentRequestIdentity
{
    public RequestIdentity GetRequired()
    {
        ClaimsPrincipal principal = accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        string tenantValue = principal.FindFirstValue("tid") ?? throw new UnauthorizedAccessException("Tenant claim is missing.");
        string userValue = principal.FindFirstValue("oid") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User claim is missing.");
        if (!Guid.TryParse(tenantValue, out Guid tenantId) || !Guid.TryParse(userValue, out Guid userId))
            throw new UnauthorizedAccessException("Identity claims are invalid.");
        HashSet<string> groups = principal.FindAll("groups").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new(tenantId, userId, groups);
    }
}
