using System.Net;
using System.Net.Http.Json;
using IdentityHub.Contracts.DTOs.Permissions.Requests;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using IdentityHub.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.IntegrationTests.Controllers;

public class AdminPermissionsEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminPermissionsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.Permissions.RemoveRange(db.Permissions);
        await db.SaveChangesAsync();
    }

    private async Task SeedPermissionAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        db.Permissions.Add(new Permission { Id = Guid.NewGuid(), Name = name });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPermissions_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/admin/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PermissionsEnvelope>();
        Assert.Equal(0, body!.Count);
    }

    [Fact]
    public async Task GetPermissions_ReturnsSeededPermissions()
    {
        await SeedPermissionAsync("users.read");
        await SeedPermissionAsync("users.write");

        var response = await _client.GetAsync("/api/admin/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PermissionsEnvelope>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetPermissionByName_ReturnsPermission_WhenExists()
    {
        await SeedPermissionAsync("roles.read");

        var response = await _client.GetAsync("/api/admin/permissions/roles.read");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPermissionByName_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/admin/permissions/no.such.permission");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePermission_ReturnsCreated_WithValidPayload()
    {
        var request = new CreatePermissionRequest { Name = "reports.export" };

        var response = await _client.PostAsJsonAsync("/api/admin/permissions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeletePermission_ReturnsNoContent_WhenExists()
    {
        await SeedPermissionAsync("temp.permission");

        var response = await _client.DeleteAsync("/api/admin/permissions/temp.permission");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePermission_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync("/api/admin/permissions/does.not.exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record PermissionsEnvelope(int Count, List<object> Permissions);
}
