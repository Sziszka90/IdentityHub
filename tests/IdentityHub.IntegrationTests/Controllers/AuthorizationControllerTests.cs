using System.Net;
using System.Net.Http.Json;
using IdentityHub.Contracts.DTOs.Permissions.Requests;
using IdentityHub.IntegrationTests.Infrastructure;
using Xunit;

namespace IdentityHub.IntegrationTests.Controllers;

public class AuthorizationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CheckPermission_ReturnsOk_WithValidRequest()
    {
        var request = new PermissionCheckRequest { Permission = "users.read" };

        var response = await _client.PostAsJsonAsync("/api/authorization/check", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckPermission_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/authorization/check", (object?)null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
