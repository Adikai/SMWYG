using System.Net.Http;
using System.Net.Http.Json;
using SMWYG.Models;

namespace SMWYG.Services
{
    public class ReactivateResult
    {
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public interface IApiService
    {
        Task<List<Server>> GetUserServersAsync(Guid userId);
        Task<List<Channel>> GetChannelsAsync(Guid serverId);
        Task<List<Message>> GetMessagesAsync(Guid channelId);
        Task<Server> CreateServerAsync(string name, Guid ownerId);
        Task<Channel> CreateChannelAsync(Guid serverId, string name, string type, string? category, int position);
        Task RenameChannelAsync(Guid channelId, string newName);
        Task<Message> SendMessageAsync(Message message);
        Task UpdateServerIconAsync(Guid serverId, string iconPath);
        Task<User?> LoginAsync(string username, string password);
        Task<User?> RegisterAsync(string inviteToken, string username, string? displayName, string password);
        Task CreateInviteTokenAsync(InviteToken token);
        Task RevokeInviteTokenAsync(Guid tokenId);
        Task<List<InviteToken>> GetInviteTokensAsync();
        Task DeleteChannelAsync(Guid channelId);
        Task UpdateServerAsync(Server server);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<List<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(string username, string password, bool isAdmin);
        Task<bool> UserExistsAsync(string username, Guid? excludeId = null);
        Task UpdateUserAsync(User user);
        Task DeactivateUserAsync(Guid id);
        Task<ReactivateResult?> ReactivateUserAsync(Guid id);
        Task DeleteUserAsync(Guid id);
        Task ReorderChannelsAsync(Guid serverId, Guid[] orderedChannelIds);
        Task<List<ServerMember>> GetServerMembersAsync(Guid serverId);
        Task AddServerMemberAsync(Guid serverId, Guid userId, string role);
        Task<List<Server>> GetAllServersAsync();
        Task<List<Message>> GetNewMessagesAsync(Guid channelId, DateTime since);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Server>> GetUserServersAsync(Guid userId)
        {
            var res = await _http.GetFromJsonAsync<List<Server>>($"api/servers/user/{userId}");
            return res ?? new List<Server>();
        }

        public async Task<List<Channel>> GetChannelsAsync(Guid serverId)
        {
            var res = await _http.GetFromJsonAsync<List<Channel>>($"api/channels/server/{serverId}");
            return res ?? new List<Channel>();
        }

        public async Task<List<Message>> GetMessagesAsync(Guid channelId)
        {
            var res = await _http.GetFromJsonAsync<List<Message>>($"api/messages/channel/{channelId}");
            return res ?? new List<Message>();
        }

        public async Task<Server> CreateServerAsync(string name, Guid ownerId)
        {
            var payload = new { Name = name, OwnerId = ownerId };
            var res = await _http.PostAsJsonAsync("api/servers", payload);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<Server>()!;
        }

        public async Task<Channel> CreateChannelAsync(Guid serverId, string name, string type, string? category, int position)
        {
            var payload = new { ServerId = serverId, Name = name, Type = type, Category = category, Position = position };
            var res = await _http.PostAsJsonAsync("api/channels", payload);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<Channel>()!;
        }

        public async Task RenameChannelAsync(Guid channelId, string newName)
        {
            var payload = new { Name = newName };
            var res = await _http.PutAsJsonAsync($"api/channels/{channelId}", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            var res = await _http.PostAsJsonAsync("api/messages", message);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<Message>()!;
        }

        public async Task UpdateServerIconAsync(Guid serverId, string iconPath)
        {
            var payload = new { Id = serverId, Icon = iconPath };
            var res = await _http.PutAsJsonAsync($"api/servers/{serverId}", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var payload = new { Username = username, Password = password };
            var res = await _http.PostAsJsonAsync("api/users/login", payload);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<User>();
        }

        public async Task<User?> RegisterAsync(string inviteToken, string username, string? displayName, string password)
        {
            var payload = new { InviteToken = inviteToken, Username = username, DisplayName = displayName, Password = password };
            var res = await _http.PostAsJsonAsync("api/users/register", payload);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<User>();
        }

        public async Task CreateInviteTokenAsync(InviteToken token)
        {
            var res = await _http.PostAsJsonAsync("api/invitetokens", token);
            res.EnsureSuccessStatusCode();
        }

        public async Task RevokeInviteTokenAsync(Guid tokenId)
        {
            var res = await _http.DeleteAsync($"api/invitetokens/{tokenId}");
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<InviteToken>> GetInviteTokensAsync()
        {
            var res = await _http.GetFromJsonAsync<List<InviteToken>>("api/invitetokens");
            return res ?? new List<InviteToken>();
        }

        public async Task DeleteChannelAsync(Guid channelId)
        {
            var res = await _http.DeleteAsync($"api/channels/{channelId}");
            res.EnsureSuccessStatusCode();
        }

        public async Task UpdateServerAsync(Server server)
        {
            var res = await _http.PutAsJsonAsync($"api/servers/{server.Id}", server);
            res.EnsureSuccessStatusCode();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<User>($"api/users/{id}");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var res = await _http.GetFromJsonAsync<List<User>>("api/users");
            return res ?? new List<User>();
        }

        public async Task<User> CreateUserAsync(string username, string password, bool isAdmin)
        {
            var payload = new { Username = username, Password = password, DisplayName = (string?)null, IsAdmin = isAdmin };
            var res = await _http.PostAsJsonAsync("api/users", payload);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<User>()!;
        }

        public async Task<bool> UserExistsAsync(string username, Guid? excludeId = null)
        {
            var users = await GetAllUsersAsync();
            return users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || u.Id != excludeId.Value));
        }

        public async Task UpdateUserAsync(User user)
        {
            var res = await _http.PutAsJsonAsync($"api/users/{user.Id}", user);
            res.EnsureSuccessStatusCode();
        }

        public async Task DeactivateUserAsync(Guid id)
        {
            var res = await _http.PostAsync($"api/users/{id}/deactivate", null);
            res.EnsureSuccessStatusCode();
        }

        public class ReactivateResultDto { public string temporaryPassword { get; set; } = string.Empty; }
        public async Task<ReactivateResult?> ReactivateUserAsync(Guid id)
        {
            var res = await _http.PostAsync($"api/users/{id}/reactivate", null);
            if (!res.IsSuccessStatusCode) return null;
            var dto = await res.Content.ReadFromJsonAsync<ReactivateResultDto>();
            return new ReactivateResult { TemporaryPassword = dto?.temporaryPassword ?? string.Empty };
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var res = await _http.DeleteAsync($"api/users/{id}");
            res.EnsureSuccessStatusCode();
        }

        public async Task ReorderChannelsAsync(Guid serverId, Guid[] orderedChannelIds)
        {
            var res = await _http.PostAsJsonAsync($"api/channels/server/{serverId}/reorder", orderedChannelIds);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<ServerMember>> GetServerMembersAsync(Guid serverId)
        {
            var res = await _http.GetFromJsonAsync<List<ServerMember>>($"api/servers/{serverId}/members");
            return res ?? new List<ServerMember>();
        }

        public async Task AddServerMemberAsync(Guid serverId, Guid userId, string role)
        {
            var payload = new { UserId = userId, Role = role };
            var res = await _http.PostAsJsonAsync($"api/servers/{serverId}/members", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<Server>> GetAllServersAsync()
        {
            var res = await _http.GetFromJsonAsync<List<Server>>("api/servers");
            return res ?? new List<Server>();
        }

        public async Task<List<Message>> GetNewMessagesAsync(Guid channelId, DateTime since)
        {
            var res = await _http.GetFromJsonAsync<List<Message>>($"api/messages/channel/{channelId}?since={since:o}");
            return res ?? new List<Message>();
        }
    }
}
