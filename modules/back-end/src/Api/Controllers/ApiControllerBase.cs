using System.Net.Mime;
using Api.Authorization;
using Api.Filters;
using Application.Bases.Exceptions;
using Application.Users;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[Authorize]
[ApiController]
[VerifyOpenApiApplicable]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerResponse(200)]
[SwaggerResponse(401)]
[SwaggerResponse(403)]
[Route("api/v{version:apiVersion}/[controller]")]
public class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator
    {
        get { return _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>(); }
    }

    private ICurrentUser? _currentUser;
    protected ICurrentUser CurrentUser
    {
        get { return _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>(); }
    }

    private Guid? _orgId;
    protected Guid OrgId
    {
        get
        {
            if (_orgId.HasValue)
            {
                return _orgId.Value;
            }

            // the organization header has been rejected by the tenant check, don't let the request act on it
            if (HttpContext.Items.ContainsKey(TenantAuthorizationFilter.InaccessibleOrgKey))
            {
                throw new ForbiddenException("The current user is not a member of the requested organization.");
            }

            var orgIdHeaderValue = HttpContext.Request.Headers[ApiConstants.OrgIdHeaderKey];

            _orgId = Guid.TryParse(orgIdHeaderValue, out var orgId)
                ? orgId
                : Guid.Empty;
            return _orgId.Value;
        }
    }
    
    private Guid? _workspaceId;
    protected Guid WorkspaceId
    {
        get
        {
            if (_workspaceId.HasValue)
            {
                return _workspaceId.Value;
            }

            var workspaceIdHeaderValue = HttpContext.Request.Headers[ApiConstants.WorkspaceHeaderKey];

            _workspaceId = Guid.TryParse(workspaceIdHeaderValue, out var workspaceId)
                ? workspaceId
                : Guid.Empty;
            return _workspaceId.Value;
        }
    }

    protected static ApiResponse<TData> Ok<TData>(TData data) => ApiResponse<TData>.Ok(data);

    protected static ApiResponse<TData> Error<TData>(string errorCode) => ApiResponse<TData>.Error(errorCode);
}