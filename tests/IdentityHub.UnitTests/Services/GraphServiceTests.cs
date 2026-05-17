using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;

namespace IdentityHub.UnitTests.Services;

/// <summary>
/// Intercepts HTTP calls made by the Graph SDK and returns pre-configured responses.
/// Entries are matched in registration order; first match wins.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Match, HttpStatusCode Status, string Body)> _entries = new();

    /// <summary>Registers a response matched by a custom predicate.</summary>
    public void Setup(Func<HttpRequestMessage, bool> match, HttpStatusCode status, string body)
        => _entries.Add((match, status, body));

    /// <summary>Registers a response matched by URL substring + HTTP method.</summary>
    public void Setup(string urlContains, HttpMethod method, HttpStatusCode status, string body)
        => Setup(r => r.RequestUri!.ToString().Contains(urlContains) && r.Method == method, status, body);

    /// <summary>Registers a response matched by URL substring (any method).</summary>
    public void Setup(string urlContains, HttpStatusCode status, string body)
        => Setup(r => r.RequestUri!.ToString().Contains(urlContains), status, body);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var (match, status, body) in _entries)
        {
            if (match(request))
                return Task.FromResult(BuildResponse(status, body));
        }

        // Default: 404 ODataError
        return Task.FromResult(BuildResponse(HttpStatusCode.NotFound, ODataNotFound("resource")));
    }

    private static HttpResponseMessage BuildResponse(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static string ODataNotFound(string resource) =>
        $"{{\"error\":{{\"code\":\"Request_ResourceNotFound\",\"message\":\"Resource '{resource}' does not exist.\",\"innerError\":{{\"date\":\"2024-01-01\",\"request-id\":\"test\",\"client-request-id\":\"test\"}}}}}}";
}

public class GraphServiceTests
{
    private readonly Mock<ILogger<GraphService>> _loggerMock = new();

    private (GraphService Service, MockHttpMessageHandler Handler) Create()
    {
        var handler = new MockHttpMessageHandler();
        var graphClient = new GraphServiceClient(new HttpClient(handler));
        return (new GraphService(graphClient, _loggerMock.Object), handler);
    }

    // -------------------------------------------------------------------------
    // GetUsersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUsersAsync_ReturnsUsers()
    {
        var (svc, handler) = Create();
        handler.Setup("/users", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users","value":[{"id":"u1","displayName":"Alice","mail":"alice@contoso.com"}]}""");

        var result = await svc.GetUsersAsync();

        Assert.Single(result);
        Assert.Equal("u1", result[0].Id);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsEmptyList_WhenNoUsersInResponse()
    {
        var (svc, handler) = Create();
        handler.Setup("/users", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users","value":[]}""");

        var result = await svc.GetUsersAsync();

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserAsync_ReturnsUser()
    {
        var (svc, handler) = Create();
        handler.Setup("/users/u1", HttpStatusCode.OK,
            """{"id":"u1","displayName":"Alice","mail":"alice@contoso.com"}""");

        var result = await svc.GetUserAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result.Id);
        Assert.Equal("Alice", result.DisplayName);
    }

    [Fact]
    public async Task GetUserAsync_ThrowsGraphResourceNotFoundException_WhenNotFound()
    {
        var (svc, _) = Create(); // default handler returns 404

        await Assert.ThrowsAsync<GraphResourceNotFoundException>(() => svc.GetUserAsync("unknown"));
    }

    // -------------------------------------------------------------------------
    // CreateUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUserAsync_ReturnsCreatedUser()
    {
        var (svc, handler) = Create();
        handler.Setup("/users", HttpMethod.Post, HttpStatusCode.Created,
            """{"id":"u2","userPrincipalName":"bob@contoso.com"}""");

        var result = await svc.CreateUserAsync(new Microsoft.Graph.Models.User { UserPrincipalName = "bob@contoso.com" });

        Assert.Equal("u2", result.Id);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenGraphReturnsNull()
    {
        var (svc, handler) = Create();
        // Graph returning 204 No Content causes Kiota to return null for the deserialized object
        handler.Setup("/users", HttpMethod.Post, HttpStatusCode.NoContent, "");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateUserAsync(new Microsoft.Graph.Models.User { UserPrincipalName = "null@contoso.com" }));
    }

    // -------------------------------------------------------------------------
    // UpdateUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateUserAsync_ThrowsException_WhenUserDoesNotExist()
    {
        var (svc, _) = Create(); // default handler returns 404 ODataError for GET

        // Kiota throws ODataError on 404 before the null-check in the service can execute
        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.UpdateUserAsync(new Microsoft.Graph.Models.User { Id = "unknown" }));
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsUpdatedUser()
    {
        var (svc, handler) = Create();

        // Both GETs return the user; PATCH returns 204
        handler.Setup(r => r.RequestUri!.ToString().Contains("/users/u1") && r.Method == HttpMethod.Get,
            HttpStatusCode.OK,
            """{"id":"u1","displayName":"Updated Name"}""");
        handler.Setup(r => r.RequestUri!.ToString().Contains("/users/u1") && r.Method == HttpMethod.Patch,
            HttpStatusCode.NoContent, "");

        var result = await svc.UpdateUserAsync(new Microsoft.Graph.Models.User { Id = "u1", DisplayName = "Updated Name" });

        Assert.NotNull(result);
        Assert.Equal("u1", result.Id);
    }

    // -------------------------------------------------------------------------
    // DeleteUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteUserAsync_CompletesWithoutException_WhenUserDeleted()
    {
        var (svc, handler) = Create();

        handler.Setup(r => r.RequestUri!.ToString().Contains("/users/u1") && r.Method == HttpMethod.Delete,
            HttpStatusCode.NoContent, "");

        // After DELETE, GET returns 404 (confirming deletion) → service treats this as success
        // Default handler returns 404 for the verification GET

        var ex = await Record.ExceptionAsync(() => svc.DeleteUserAsync("u1"));

        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // GetUserDirectGroupIdsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserDirectGroupIdsAsync_ReturnsGroupIds()
    {
        var (svc, handler) = Create();
        handler.Setup("/users/u1/memberOf", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#directoryObjects","value":[{"@odata.type":"#microsoft.graph.group","id":"g1"},{"@odata.type":"#microsoft.graph.group","id":"g2"}]}""");

        var result = await svc.GetUserDirectGroupIdsAsync("u1");

        Assert.Equal(2, result.Count);
        Assert.Contains("g1", result);
        Assert.Contains("g2", result);
    }

    [Fact]
    public async Task GetUserDirectGroupIdsAsync_ThrowsGraphResourceNotFoundException_WhenUserNotFound()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<GraphResourceNotFoundException>(() => svc.GetUserDirectGroupIdsAsync("unknown"));
    }

    // -------------------------------------------------------------------------
    // GetUserTransitiveGroupIdsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserTransitiveGroupIdsAsync_ReturnsGroupIds()
    {
        var (svc, handler) = Create();
        handler.Setup("/users/u1/transitiveMemberOf", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#directoryObjects","value":[{"@odata.type":"#microsoft.graph.group","id":"g1"}]}""");

        var result = await svc.GetUserTransitiveGroupIdsAsync("u1");

        Assert.Single(result);
        Assert.Equal("g1", result[0]);
    }

    [Fact]
    public async Task GetUserTransitiveGroupIdsAsync_ReturnsEmpty_WhenUserHasNoGroups()
    {
        var (svc, handler) = Create();
        handler.Setup("/users/u1/transitiveMemberOf", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#directoryObjects","value":[]}""");

        var result = await svc.GetUserTransitiveGroupIdsAsync("u1");

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetGroupAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGroupAsync_ReturnsGroup()
    {
        var (svc, handler) = Create();
        handler.Setup("/groups/g1", HttpStatusCode.OK,
            """{"id":"g1","displayName":"Admins","mailNickname":"admins"}""");

        var result = await svc.GetGroupByIdAsync("g1");

        Assert.NotNull(result);
        Assert.Equal("g1", result.Id);
        Assert.Equal("Admins", result.DisplayName);
    }

    [Fact]
    public async Task GetGroupAsync_ThrowsGraphResourceNotFoundException_WhenNotFound()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<GraphResourceNotFoundException>(() => svc.GetGroupByIdAsync("nonexistent"));
    }

    // -------------------------------------------------------------------------
    // CreateGroupAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateGroupAsync_ReturnsCreatedGroup()
    {
        var (svc, handler) = Create();
        handler.Setup("/groups", HttpMethod.Post, HttpStatusCode.Created,
            """{"id":"g2","displayName":"Editors","mailNickname":"editors"}""");

        var result = await svc.CreateGroupAsync("Editors", "editors");

        Assert.NotNull(result);
        Assert.Equal("g2", result.Id);
    }

    // -------------------------------------------------------------------------
    // GetGroupMembersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGroupMembersAsync_ReturnsMemberIds()
    {
        var (svc, handler) = Create();
        handler.Setup("/groups/g1/members", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#directoryObjects","value":[{"id":"u1"},{"id":"u2"}]}""");

        var result = await svc.GetGroupMembersAsync("g1");

        Assert.Equal(2, result.Count);
        Assert.Contains("u1", result);
    }

    [Fact]
    public async Task GetGroupMembersAsync_ThrowsGraphResourceNotFoundException_WhenGroupNotFound()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<GraphResourceNotFoundException>(() => svc.GetGroupMembersAsync("nonexistent"));
    }

    // -------------------------------------------------------------------------
    // IsAvailableAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue_WhenGraphIsReachable()
    {
        var (svc, handler) = Create();
        handler.Setup("/users", HttpStatusCode.OK,
            """{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users","value":[{"id":"u1"}]}""");

        var result = await svc.IsAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalse_WhenGraphThrows()
    {
        var (svc, handler) = Create();
        handler.Setup("/users", HttpStatusCode.InternalServerError,
            """{"error":{"code":"ServiceNotAvailable","message":"Service unavailable."}}""");

        var result = await svc.IsAvailableAsync();

        Assert.False(result);
    }
}
