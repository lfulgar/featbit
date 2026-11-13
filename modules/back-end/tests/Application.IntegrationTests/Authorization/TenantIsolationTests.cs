using System.Net;
using Api;

namespace Application.IntegrationTests.Authorization;

/// <summary>
/// A portal (JWT) user must only reach data inside its own workspace and the organizations it belongs to,
/// regardless of the Workspace/Organization headers or the route resource ids it sends.
/// </summary>
[Collection(nameof(TestApp))]
public class TenantIsolationTests
{
    private readonly TestApp _app;

    public TenantIsolationTests(TestApp app)
    {
        _app = app;
    }

    private static Dictionary<string, string> Headers(Guid workspaceId, Guid orgId) => new()
    {
        [ApiConstants.WorkspaceHeaderKey] = workspaceId.ToString(),
        [ApiConstants.OrgIdHeaderKey] = orgId.ToString()
    };

    private static readonly Dictionary<string, string> OwnTenant = Headers(TestWorkspace.Id, TestTenants.OrgA.Id);

    [Fact]
    public async Task OwnOrganization_ListProjects_Succeeds()
    {
        var response = await _app.SendAsync(HttpMethod.Get, "api/v1/projects", OwnTenant);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TestTenants.ProjectA.Name, body);
    }

    [Fact]
    public async Task OwnEnvironment_Get_Succeeds()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectA.Id}/envs/{TestTenants.EnvA.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TestTenants.EnvA.Name, body);
    }

    [Fact]
    public async Task OwnEnvironment_WithoutTenantHeaders_Succeeds()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectA.Id}/envs/{TestTenants.EnvA.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OtherWorkspaceHeader_IsForbidden()
    {
        var headers = Headers(TestTenants.OtherWorkspaceId, TestTenants.OrgA.Id);

        var response = await _app.SendAsync(HttpMethod.Get, "api/v1/projects", headers);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherWorkspaceOrganizationHeader_IsForbidden()
    {
        // a user of workspace A claiming to act in workspace B / organization B
        var headers = Headers(TestTenants.OtherWorkspaceId, TestTenants.OrgB.Id);

        var response = await _app.SendAsync(HttpMethod.Get, "api/v1/projects", headers);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(TestTenants.ProjectB.Name, body);
    }

    [Fact]
    public async Task NonMemberOrganizationHeader_IsForbidden()
    {
        // correct workspace header, but an organization the user is not a member of
        var headers = Headers(TestWorkspace.Id, TestTenants.OrgB.Id);

        var response = await _app.SendAsync(HttpMethod.Get, "api/v1/projects", headers);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(TestTenants.ProjectB.Name, body);
    }

    [Fact]
    public async Task NonMemberOrganizationHeader_OrganizationAgnosticEndpoint_Succeeds()
    {
        // e.g. the portal still sends a current organization the user has been removed from;
        // endpoints that don't act on the organization must keep working so the portal can recover
        var headers = Headers(TestWorkspace.Id, TestTenants.OrgB.Id);

        var response = await _app.SendAsync(HttpMethod.Get, "api/v1/user/profile", headers);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OtherTenantEnvironmentRoute_IsForbidden()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectB.Id}/envs/{TestTenants.EnvB.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(TestTenants.EnvB.Name, body);
    }

    [Fact]
    public async Task OtherTenantEnvironment_UnderOwnProjectRoute_IsForbidden()
    {
        // own project id in the route does not make another tenant's env id accessible
        var uri = $"api/v1/projects/{TestTenants.ProjectA.Id}/envs/{TestTenants.EnvB.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(TestTenants.EnvB.Name, body);
    }

    [Fact]
    public async Task OtherTenantEnvironmentRoute_WithoutTenantHeaders_IsForbidden()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectB.Id}/envs/{TestTenants.EnvB.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EnvironmentOfAnotherOwnOrganization_MustMatchOrganizationHeader()
    {
        // the user is a member of OrgA2, but the request is made in the context of OrgA
        var uri = $"api/v1/projects/{TestTenants.ProjectA2.Id}/envs/{TestTenants.EnvA2.Id}";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("feature-flags")]
    [InlineData("segments")]
    [InlineData("audit-logs")]
    [InlineData("end-user-properties")]
    public async Task OtherTenantEnvScopedApis_AreForbidden(string resource)
    {
        var uri = $"api/v1/envs/{TestTenants.EnvB.Id}/{resource}";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEnvironmentRoute_IsForbidden()
    {
        var uri = $"api/v1/envs/{Guid.NewGuid()}/feature-flags";

        var response = await _app.SendAsync(HttpMethod.Get, uri, OwnTenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherTenantProject_Delete_IsForbidden()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectB.Id}";

        var response = await _app.SendAsync(HttpMethod.Delete, uri, OwnTenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherTenantEnvironment_DeleteUnderOwnProject_IsForbidden()
    {
        var uri = $"api/v1/projects/{TestTenants.ProjectA.Id}/envs/{TestTenants.EnvB.Id}";

        var response = await _app.SendAsync(HttpMethod.Delete, uri, OwnTenant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
