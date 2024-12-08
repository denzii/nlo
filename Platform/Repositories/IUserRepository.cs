using Platform.Models;

namespace Platform.Repositories
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByConnectionIdAsync(string connectionId);
        Task<bool> AssignConnectionAsync(int userId, string connectionId);
        Task<bool> RemoveConnectionAsync(string connectionId);
        Task<int> IncrementScratchCountAsync(int userId);
        Task<bool> ResetScratchCountsAsync();
    }
}
