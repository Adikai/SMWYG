using System.Net.Http.Json;
using System.Text.Json;
using SMWYG.Api.DTOs;

namespace SMWYG.Api.ApiClients
{
    public class UsersClient : IUsersClient
    {
        private readonly HttpClient _http;

        public UsersClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
        {
            var res = await _http.GetFromJsonAsync<IEnumerable<UserDto>>("api/users", ct);
            return res ?? Array.Empty<UserDto>();
        }

        public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<UserDto>($"api/users/{id}", ct);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto create, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/users", create, ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct)!;
        }

        public async Task<string> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/users/login", new { Username = username, Password = password }, ct);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (body.TryGetProperty("token", out var token))
                return token.GetString() ?? string.Empty;
            return string.Empty;
        }
    }
}
