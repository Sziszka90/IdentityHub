using IdentityHub.Application.Interfaces;
using Microsoft.Graph.Models;

namespace IdentityHub.IntegrationTests.Infrastructure.Fakes;

/// <summary>
/// In-memory fake for IGraphService, removes the Azure AD dependency in integration tests.
/// </summary>
public class FakeGraphService : IGraphService
{
    private readonly List<User> _users = new()
    {
        new User
        {
            Id = TestAuthHandler.TestUserId,
            DisplayName = "Test User",
            UserPrincipalName = "testuser@test.onmicrosoft.com",
            Mail = "testuser@test.onmicrosoft.com",
            AccountEnabled = true,
        }
    };

    private readonly List<Group> _groups = new();
    private readonly Dictionary<string, List<string>> _userGroups = new();
    private readonly Dictionary<string, List<string>> _groupMembers = new();

    public Task<List<User>> GetUsersAsync(int top = 100, int skip = 0)
        => Task.FromResult(_users.Skip(skip).Take(top).ToList());

    public Task<User?> GetUserAsync(string userId)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));

    public Task<User> CreateUserAsync(User user)
    {
        user.Id ??= Guid.NewGuid().ToString();
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<User> UpdateUserAsync(User user, string userId)
    {
        var existing = _users.FirstOrDefault(u => u.Id == userId);
        if (existing is not null)
        {
            _users.Remove(existing);
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task DeleteUserAsync(string userId)
    {
        _users.RemoveAll(u => u.Id == userId);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetUserDirectGroupIdsAsync(string userId)
        => Task.FromResult(_userGroups.TryGetValue(userId, out var groups) ? groups : new List<string>());

    public Task<List<string>> GetUserTransitiveGroupIdsAsync(string userId)
        => Task.FromResult(_userGroups.TryGetValue(userId, out var groups) ? groups : new List<string>());

    public Task<Group?> GetGroupByIdAsync(string groupId)
        => Task.FromResult(_groups.FirstOrDefault(g => g.Id == groupId));

    public Task<List<Group>> QueryGroupsAsync(string? displayName = null)
    {
        var result = displayName is null
            ? _groups
            : _groups.Where(g => g.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true).ToList();
        return Task.FromResult(result);
    }

    public Task<Group> CreateGroupAsync(string displayName, string mailNickname)
    {
        var group = new Group { Id = Guid.NewGuid().ToString(), DisplayName = displayName, MailNickname = mailNickname };
        _groups.Add(group);
        return Task.FromResult(group);
    }

    public Task<Group> UpdateGroupAsync(string groupId, string displayName, string mailNickname)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group is not null)
        {
            group.DisplayName = displayName;
            group.MailNickname = mailNickname;
        }
        return Task.FromResult(group ?? new Group { Id = groupId, DisplayName = displayName });
    }

    public Task DeleteGroupAsync(string groupId)
    {
        _groups.RemoveAll(g => g.Id == groupId);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetGroupMembersAsync(string groupId)
        => Task.FromResult(_groupMembers.TryGetValue(groupId, out var members) ? members : new List<string>());

    public Task AddUserToGroupsAsync(string userId, List<string> groupIds)
    {
        if (!_userGroups.ContainsKey(userId))
            _userGroups[userId] = new List<string>();
        _userGroups[userId].AddRange(groupIds.Except(_userGroups[userId]));

        foreach (var gid in groupIds)
        {
            if (!_groupMembers.ContainsKey(gid))
                _groupMembers[gid] = new List<string>();
            if (!_groupMembers[gid].Contains(userId))
                _groupMembers[gid].Add(userId);
        }
        return Task.CompletedTask;
    }

    public Task RemoveUserFromGroupsAsync(string userId, List<string> groupIds)
    {
        if (_userGroups.ContainsKey(userId))
            _userGroups[userId].RemoveAll(groupIds.Contains);

        foreach (var gid in groupIds)
        {
            if (_groupMembers.ContainsKey(gid))
                _groupMembers[gid].Remove(userId);
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
}
