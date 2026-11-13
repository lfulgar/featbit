using Api.Authentication;
using Api.Controllers;
using Application.Bases;
using Application.Services;
using Domain.Users;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Authorization;

/// <summary>
/// Confines portal (JWT) users to their own tenant:
/// <list type="bullet">
/// <item>the <c>Workspace</c> header must match the token's workspace claim</item>
/// <item>the user must be a member of the organization in the <c>Organization</c> header; otherwise reading
/// <see cref="ApiControllerBase"/>.OrgId fails with 403 (enforced lazily so that organization-agnostic endpoints,
/// e.g. the ones the portal calls to recover from a stale current organization, keep working)</item>
/// <item>the <c>envId</c>/<c>projectId</c> route values must belong to that organization
/// (or, when no organization header is sent, to an organization the user is a member of)</item>
/// </list>
/// </summary>
public class TenantAuthorizationFilter : IAsyncAuthorizationFilter
{
    public const string InaccessibleOrgKey = "tenant:inaccessible-org";

    private const string EnvIdRouteKey = "envId";
    private const string ProjectIdRouteKey = "projectId";

    private readonly IOrganizationService _organizationService;
    private readonly IProjectService _projectService;
    private readonly IEnvironmentService _environmentService;

    public TenantAuthorizationFilter(
        IOrganizationService organizationService,
        IProjectService projectService,
        IEnvironmentService environmentService)
    {
        _organizationService = organizationService;
        _projectService = projectService;
        _environmentService = environmentService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var principal = context.HttpContext.User;
        if (principal.Identity is not { IsAuthenticated: true, AuthenticationType: Schemes.JwtBearer })
        {
            return;
        }

        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        if (!Guid.TryParse(principal.FindFirst(UserClaims.Id)?.Value, out var userId) ||
            !Guid.TryParse(principal.FindFirst(UserClaims.WorkspaceId)?.Value, out var tokenWorkspaceId))
        {
            context.Result = Error(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized);
            return;
        }

        var headers = context.HttpContext.Request.Headers;

        // header values are parsed the same way as ApiControllerBase does
        if (Guid.TryParse(headers[ApiConstants.WorkspaceHeaderKey], out var workspaceId) &&
            workspaceId != tokenWorkspaceId)
        {
            context.Result = Forbidden();
            return;
        }

        Guid? orgId = Guid.TryParse(headers[ApiConstants.OrgIdHeaderKey], out var parsedOrgId) ? parsedOrgId : null;

        // organizations owning the resources referenced in the route
        var routeOrgIds = new List<Guid>();
        var routeValues = context.RouteData.Values;
        if (TryGetRouteGuid(routeValues, EnvIdRouteKey, out var envId))
        {
            var descriptor = await _environmentService.GetResourceDescriptorAsync(envId);
            if (descriptor?.Organization is null)
            {
                context.Result = Forbidden();
                return;
            }

            routeOrgIds.Add(descriptor.Organization.Id);
        }

        if (TryGetRouteGuid(routeValues, ProjectIdRouteKey, out var projectId))
        {
            var project = await _projectService.FindOneAsync(x => x.Id == projectId);
            if (project is null)
            {
                context.Result = Forbidden();
                return;
            }

            routeOrgIds.Add(project.OrganizationId);
        }

        if (orgId is null && routeOrgIds.Count == 0)
        {
            return;
        }

        var memberOrgIds = (await _organizationService.GetListAsync(userId))
            .Where(x => x.WorkspaceId == tokenWorkspaceId)
            .Select(x => x.Id)
            .ToHashSet();

        if (orgId.HasValue && !memberOrgIds.Contains(orgId.Value))
        {
            context.HttpContext.Items[InaccessibleOrgKey] = true;
        }

        foreach (var routeOrgId in routeOrgIds)
        {
            if (!memberOrgIds.Contains(routeOrgId) || (orgId.HasValue && routeOrgId != orgId.Value))
            {
                context.Result = Forbidden();
                return;
            }
        }
    }

    private static bool TryGetRouteGuid(RouteValueDictionary values, string key, out Guid id)
    {
        id = Guid.Empty;
        return values.TryGetValue(key, out var value) && Guid.TryParse(value?.ToString(), out id);
    }

    private static ObjectResult Forbidden() => Error(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden);

    private static ObjectResult Error(int statusCode, string errorCode) =>
        new(ApiResponse<object>.Error(errorCode)) { StatusCode = statusCode };
}
