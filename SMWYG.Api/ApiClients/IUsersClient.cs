using SMWYG.Api.DTOs;

namespace SMWYG.Api.ApiClients
{
    public interface IUsersClient
    {
        Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default);
        Task<UserDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<UserDto> CreateAsync(CreateUserDto create, CancellationToken ct = default);
        Task<string> LoginAsync(string username, string password, CancellationToken ct = default);
    }
}
