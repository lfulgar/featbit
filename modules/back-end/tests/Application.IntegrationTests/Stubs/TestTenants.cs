using Domain.Organizations;
using Domain.Projects;
using Environment = Domain.Environments.Environment;

namespace Application.IntegrationTests.Stubs;

/// <summary>
/// Test tenancy layout:
/// <list type="bullet">
/// <item>workspace <see cref="TestWorkspace.Id"/> (the test user's workspace) owns <see cref="OrgA"/> and <see cref="OrgA2"/>;
/// the test user is a member of both</item>
/// <item><see cref="OtherWorkspaceId"/> is another tenant's workspace that owns <see cref="OrgB"/>;
/// the test user is NOT a member of it</item>
/// </list>
/// Each organization has one project with one environment.
/// </summary>
public static class TestTenants
{
    public static readonly Guid OtherWorkspaceId = new("0b82c894-d9b5-4846-99e9-ae27ab7d247d");

    public static readonly Organization OrgA = NewOrg("a0000000-0000-0000-0000-000000000001", TestWorkspace.Id, "org-a");
    public static readonly Organization OrgA2 = NewOrg("a0000000-0000-0000-0000-000000000002", TestWorkspace.Id, "org-a2");
    public static readonly Organization OrgB = NewOrg("b0000000-0000-0000-0000-000000000001", OtherWorkspaceId, "org-b");

    public static readonly Project ProjectA = NewProject("a1000000-0000-0000-0000-000000000001", OrgA, "project-a");
    public static readonly Project ProjectA2 = NewProject("a1000000-0000-0000-0000-000000000002", OrgA2, "project-a2");
    public static readonly Project ProjectB = NewProject("b1000000-0000-0000-0000-000000000001", OrgB, "project-b");

    public static readonly Environment EnvA = NewEnv("a2000000-0000-0000-0000-000000000001", ProjectA, "env-a");
    public static readonly Environment EnvA2 = NewEnv("a2000000-0000-0000-0000-000000000002", ProjectA2, "env-a2");
    public static readonly Environment EnvB = NewEnv("b2000000-0000-0000-0000-000000000001", ProjectB, "env-b");

    public static readonly Organization[] Organizations = { OrgA, OrgA2, OrgB };
    public static readonly Project[] Projects = { ProjectA, ProjectA2, ProjectB };
    public static readonly Environment[] Environments = { EnvA, EnvA2, EnvB };

    // organization memberships of the test user
    public static readonly Guid[] TestUserOrganizationIds = { OrgA.Id, OrgA2.Id };

    private static Organization NewOrg(string id, Guid workspaceId, string key) =>
        new(workspaceId, key, key) { Id = new Guid(id) };

    private static Project NewProject(string id, Organization org, string key) =>
        new(org.Id, key, key) { Id = new Guid(id) };

    private static Environment NewEnv(string id, Project project, string key) =>
        new(project.Id, key, key) { Id = new Guid(id) };
}
