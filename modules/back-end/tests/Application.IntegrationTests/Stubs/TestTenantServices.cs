using System.Linq.Expressions;
using Application.Bases.Exceptions;
using Application.Services;
using Domain.Environments;
using Domain.Organizations;
using Domain.Projects;
using Environment = Domain.Environments.Environment;

namespace Application.IntegrationTests.Stubs;

public class TestOrganizationService : NullServiceBase<Organization>, IOrganizationService
{
    public override Task<Organization> GetAsync(Guid id) =>
        Task.FromResult(TestTenants.Organizations.FirstOrDefault(x => x.Id == id) ??
                        throw new EntityNotFoundException(nameof(Organization), id.ToString()));

    public override Task<Organization?> FindOneAsync(Expression<Func<Organization, bool>> predicate) =>
        Task.FromResult(TestTenants.Organizations.FirstOrDefault(predicate.Compile()));

    public Task<ICollection<Organization>> GetListAsync(Guid userId)
    {
        ICollection<Organization> orgs = userId == TestUser.Id
            ? TestTenants.Organizations.Where(x => TestTenants.TestUserOrganizationIds.Contains(x.Id)).ToArray()
            : Array.Empty<Organization>();

        return Task.FromResult(orgs);
    }

    public Task<string[]> GetScopesAsync(ScopeString[] scopeStrings) => throw new NotImplementedException();
    public Task<bool> HasKeyBeenUsedAsync(Guid workspaceId, string key) => throw new NotImplementedException();

    public Task AddUserAsync(
        OrganizationUser organizationUser,
        ICollection<Guid>? policies = null,
        ICollection<Guid>? groups = null) => throw new NotImplementedException();

    public Task RemoveUserAsync(Guid organizationId, Guid userId) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}

public class TestProjectService : NullServiceBase<Project>, IProjectService
{
    public override Task<Project> GetAsync(Guid id) =>
        Task.FromResult(TestTenants.Projects.FirstOrDefault(x => x.Id == id) ??
                        throw new EntityNotFoundException(nameof(Project), id.ToString()));

    public override Task<Project?> FindOneAsync(Expression<Func<Project, bool>> predicate) =>
        Task.FromResult(TestTenants.Projects.FirstOrDefault(predicate.Compile()));

    public Task<IEnumerable<ProjectWithEnvs>> GetListAsync(Guid organizationId)
    {
        var projects = TestTenants.Projects
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new ProjectWithEnvs
            {
                Id = x.Id,
                Name = x.Name,
                Key = x.Key,
                Environments = TestTenants.Environments.Where(env => env.ProjectId == x.Id).ToArray()
            });

        return Task.FromResult(projects);
    }

    public Task<ProjectWithEnvs> GetWithEnvsAsync(Guid id) => throw new NotImplementedException();

    public Task<ProjectWithEnvs> AddWithEnvsAsync(Project project, IEnumerable<string> envNames) =>
        throw new NotImplementedException();

    public Task<bool> HasKeyBeenUsedAsync(Guid organizationId, string key) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(Guid id) => throw new NotImplementedException();
    public Task DeleteManyAsync(ICollection<Guid> projectIds) => throw new NotImplementedException();
}

public class TestEnvironmentService : NullServiceBase<Environment>, IEnvironmentService
{
    public override Task<Environment> GetAsync(Guid id) =>
        Task.FromResult(TestTenants.Environments.FirstOrDefault(x => x.Id == id) ??
                        throw new EntityNotFoundException(nameof(Environment), id.ToString()));

    public override Task<Environment?> FindOneAsync(Expression<Func<Environment, bool>> predicate) =>
        Task.FromResult(TestTenants.Environments.FirstOrDefault(predicate.Compile()));

    public Task<ResourceDescriptor> GetResourceDescriptorAsync(Guid envId)
    {
        var env = TestTenants.Environments.FirstOrDefault(x => x.Id == envId);
        if (env == null)
        {
            return Task.FromResult<ResourceDescriptor>(null!);
        }

        var project = TestTenants.Projects.First(x => x.Id == env.ProjectId);
        var org = TestTenants.Organizations.First(x => x.Id == project.OrganizationId);

        var descriptor = new ResourceDescriptor
        {
            Organization = new IdNameKeyProps { Id = org.Id, Name = org.Name, Key = org.Key },
            Project = new IdNameKeyProps { Id = project.Id, Name = project.Name, Key = project.Key },
            Environment = new IdNameKeyProps { Id = env.Id, Name = env.Name, Key = env.Key }
        };

        return Task.FromResult(descriptor);
    }

    public Task AddWithBuiltInPropsAsync(Environment env) => throw new NotImplementedException();
    public Task AddManyWithBuiltInPropsAsync(ICollection<Environment> envs) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
    public Task DeleteManyAsync(ICollection<Guid> ids) => throw new NotImplementedException();
    public Task<IEnumerable<Setting>> GetSettingsAsync(Guid envId, string type) => throw new NotImplementedException();
    public Task<bool> HasKeyBeenUsedAsync(Guid projectId, string key) => throw new NotImplementedException();
}
