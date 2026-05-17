using System.Net;
using System.Net.Http.Json;
using IdentityHub.Contracts.DTOs.Groups.Requests;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using IdentityHub.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.IntegrationTests.Controllers;

public class AdminGroupMappingsEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminGroupMappingsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.GroupRoleMappings.RemoveRange(db.GroupRoleMappings);
        db.Roles.RemoveRange(db.Roles);
        await db.SaveChangesAsync();
    }

    private async Task<(Guid RoleId, Guid MappingId)> SeedMappingAsync(string groupName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();

        var role = new Role { Id = Guid.NewGuid(), Name = $"role-for-{groupName}", Description = string.Empty };
        db.Roles.Add(role);

        var mapping = new GroupRoleMapping { Id = Guid.NewGuid(), GroupName = groupName, RoleId = role.Id };
        db.GroupRoleMappings.Add(mapping);

        await db.SaveChangesAsync();
        return (role.Id, mapping.Id);
    }

    [Fact]
    public async Task GetGroupRoleMappings_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/admin/group-role-mappings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MappingsEnvelope>();
        Assert.Equal(0, body!.Count);
    }

    [Fact]
    public async Task GetGroupRoleMappings_ReturnsSeededMappings()
    {
        await SeedMappingAsync("EngineeringGroup");
        await SeedMappingAsync("DesignGroup");

        var response = await _client.GetAsync("/api/admin/group-role-mappings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MappingsEnvelope>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetGroupRoleMappingByGroupName_ReturnsMapping_WhenExists()
    {
        await SeedMappingAsync("ProductGroup");

        var response = await _client.GetAsync("/api/admin/group-role-mappings/ProductGroup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupRoleMappingByGroupName_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/admin/group-role-mappings/NoSuchGroup");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroupRoleMapping_ReturnsCreated_WithValidPayload()
    {
        // Seed the role that the mapping points to
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        var role = new Role { Id = Guid.NewGuid(), Name = "target-role", Description = string.Empty };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var request = new CreateGroupRequest
        {
            GroupName = "SalesGroup",
            RoleId = role.Id.ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/admin/group-role-mappings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroupRoleMapping_ReturnsBadRequest_WhenRoleIdIsInvalidGuid()
    {
        var request = new CreateGroupRequest { GroupName = "SomeGroup", RoleId = "not-a-guid" };

        var response = await _client.PostAsJsonAsync("/api/admin/group-role-mappings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroupRoleMapping_ReturnsNoContent_WhenExists()
    {
        var (_, mappingId) = await SeedMappingAsync("TempGroup");

        var response = await _client.DeleteAsync($"/api/admin/group-role-mappings/{mappingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroupRoleMapping_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync($"/api/admin/group-role-mappings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroupRoleMapping_ReturnsBadRequest_WhenIdIsNotGuid()
    {
        var response = await _client.DeleteAsync("/api/admin/group-role-mappings/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record MappingsEnvelope(int Count, List<object> GroupRoleMappings);
}
