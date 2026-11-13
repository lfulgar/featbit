using Api.Authentication;
using Api.Authorization;
using Application.Projects;
using Domain.Projects;

namespace Api.Controllers;

[Route("api/v{version:apiVersion}/projects")]
public class ProjectController : ApiControllerBase
{
    [OpenApi]
    [HttpGet]
    [Route("{projectId:guid}")]
    [Authorize(Permissions.CanAccessProject)]
    public async Task<ApiResponse<ProjectWithEnvs>> GetAsync(Guid projectId)
    {
        var request = new GetProject
        {
            Id = projectId
        };

        var project = await Mediator.Send(request);
        return Ok(project);
    }

    [OpenApi]
    [HttpGet]
    [Authorize(Permissions.CanAccessProject)]
    public async Task<ApiResponse<IEnumerable<ProjectWithEnvs>>> GetListAsync()
    {
        var request = new GetProjectList
        {
            OrganizationId = OrgId
        };

        var projects = await Mediator.Send(request);
        return Ok(projects);
    }

    [HttpPost]
    public async Task<ApiResponse<ProjectWithEnvs>> CreateAsync(CreateProject request)
    {
        request.OrganizationId = OrgId;

        var projectWithEnvs = await Mediator.Send(request);
        return Ok(projectWithEnvs);
    }

    [HttpPut("{projectId:guid}")]
    public async Task<ApiResponse<ProjectVm>> UpdateAsync(Guid projectId, UpdateProject request)
    {
        request.Id = projectId;

        var project = await Mediator.Send(request);
        return Ok(project);
    }

    [HttpDelete]
    [Route("{projectId:guid}")]
    public async Task<ApiResponse<bool>> DeleteAsync(Guid projectId)
    {
        var request = new DeleteProject
        {
            Id = projectId
        };

        var success = await Mediator.Send(request);
        return Ok(success);
    }

    [HttpGet("is-key-used")]
    public async Task<ApiResponse<bool>> IsKeyUsedAsync(string key)
    {
        var request = new IsKeyUsed
        {
            OrganizationId = OrgId,
            Key = key
        };

        var isUsed = await Mediator.Send(request);
        return Ok(isUsed);
    }
}