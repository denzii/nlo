using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;

namespace Platform.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly GridDbContext _context;

        public UserRepository(GridDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByConnectionIdAsync(string connectionId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ConnectionId == connectionId);
        }

        public async Task<bool> AssignConnectionAsync(int userId, string connectionId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"Error: UserId={userId} does not exist.");
                return false;
            }

            user.ConnectionId = connectionId;
            await _context.SaveChangesAsync();

            Console.WriteLine($"Successfully assigned ConnectionId={connectionId} to UserId={userId}.");
            return true;
        }

        public async Task<bool> RemoveConnectionAsync(string connectionId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ConnectionId == connectionId);
            if (user == null)
            {
                Console.WriteLine($"Warning: No user found with ConnectionId={connectionId}.");
                return false;
            }

            user.ConnectionId = null;
            await _context.SaveChangesAsync();

            Console.WriteLine($"Successfully cleared ConnectionId={connectionId} for UserId={user.Id}.");
            return true;
        }

        public async Task<int> IncrementScratchCountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"Error: UserId={userId} does not exist.");
                return user.ScratchCount;
            }
            if (user.ScratchCount >= 1) // Limit to 1 scratch
            {
                Console.WriteLine($"UserId={userId} has already used their scratch.");
                user.ScratchCount++;
                return user.ScratchCount;
            }

            user.ScratchCount++;
            await _context.SaveChangesAsync();

            Console.WriteLine($"Incremented ScratchCount for UserId={userId}. Current ScratchCount={user.ScratchCount}.");
            return user.ScratchCount;
        }
        public async Task<bool> ResetScratchCountsAsync(){
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                user.ScratchCount = 0;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HandleScratchAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"Error: UserId={userId} does not exist.");
                return false;
            }

            if (user.ScratchCount >= 1)
            {
                Console.WriteLine($"Error: UserId={userId} has no remaining scratches.");
                return false;
            }

            // Increment the scratch count and save changes
            user.ScratchCount++;
            await _context.SaveChangesAsync();

            Console.WriteLine($"UserId={userId} scratched successfully.");
            return true;
        }
    }
}
