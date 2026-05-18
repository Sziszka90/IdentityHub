using System.Net;
using System.Net.Http.Json;
using IdentityHub.Contracts.DTOs.Roles.Requests;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using IdentityHub.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.IntegrationTests.Controllers;

public class AdminRolesEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminRolesEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Clean database state before each test so seeded data doesn't interfere
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.RolePermissions.RemoveRange(db.RolePermissions);
        db.GroupRoleMappings.RemoveRange(db.GroupRoleMappings);
        db.Roles.RemoveRange(db.Roles);
        db.Permissions.RemoveRange(db.Permissions);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Reset database state between tests
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.Roles.RemoveRange(db.Roles);
        db.Permissions.RemoveRange(db.Permissions);
        db.RolePermissions.RemoveRange(db.RolePermissions);
        await db.SaveChangesAsync();
    }

    private async Task SeedRoleAsync(string name, string? description = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Name = name, Description = description ?? string.Empty });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRoles_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RolesEnvelope>();
        Assert.NotNull(body);
        Assert.Equal(0, body!.Count);
    }

    [Fact]
    public async Task GetRoles_ReturnsSeededRoles()
    {
        await SeedRoleAsync("admin");
        await SeedRoleAsync("reader");

        var response = await _client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RolesEnvelope>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetRoleByName_ReturnsRole_WhenExists()
    {
        await SeedRoleAsync("editor");

        var response = await _client.GetAsync("/api/admin/roles/editor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRoleByName_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/admin/roles/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_ReturnsCreated_WithValidPayload()
    {
        var request = new CreateRoleRequest
        {
            Name = "superadmin",
            Description = "Super administrator role",
            Permissions = new List<string> { "admin.access" }
        };

        var response = await _client.PostAsJsonAsync("/api/admin/roles", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_ReturnsBadRequest_WhenNameMissing()
    {
        var request = new { description = "no name" };

        var response = await _client.PostAsJsonAsync("/api/admin/roles", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_ReturnsNoContent_WhenExists()
    {
        await SeedRoleAsync("deleteme");

        var response = await _client.DeleteAsync("/api/admin/roles/deleteme");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync("/api/admin/roles/ghost");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record RolesEnvelope(int Count, List<object> Roles);
}
