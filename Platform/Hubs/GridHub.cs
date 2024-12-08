using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Platform.Repositories;
using System.Collections.Concurrent;
using Platform.Models;
namespace Platform.Hubs
{
    public class GridHub : Hub
    {
        private readonly IMemoryCache _cache;
        private readonly IGridRepository _gridRepository;
        private readonly IUserRepository _userRepository;

        public GridHub(IMemoryCache cache, IGridRepository gridRepository, IUserRepository userRepository)
        {
            _cache = cache;
            _gridRepository = gridRepository;
            _userRepository = userRepository;
        }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client attempting to connect: {Context.ConnectionId}");

        var availableUser = (await _userRepository.GetAllUsersAsync())
            .FirstOrDefault(u => string.IsNullOrEmpty(u.ConnectionId));

        if (availableUser == null)
        {
            Console.WriteLine($"Connection {Context.ConnectionId} rejected: No available user slots.");
            await Clients.Caller.SendAsync("RejectConnection", "All users are active. Try again later.");
            Context.Abort();
            return;
        }

        var success = await _userRepository.AssignConnectionAsync(availableUser.Id, Context.ConnectionId);
        if (!success)
        {
            Console.WriteLine($"Error: Could not assign connection {Context.ConnectionId} to UserId={availableUser.Id}.");
            await Clients.Caller.SendAsync("RejectConnection", "An error occurred while assigning the user slot.");
            Context.Abort();
            return;
        }

        Console.WriteLine($"Client connected: {Context.ConnectionId} assigned to UserId={availableUser.Id}");
        await Clients.Caller.SendAsync("UserAssigned", availableUser.Id);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");

        var success = await _userRepository.RemoveConnectionAsync(Context.ConnectionId);
        if (success)
        {
            Console.WriteLine($"Connection {Context.ConnectionId} successfully cleared.");
            var connections = _cache.Get<List<object>>("connections") ?? new List<object>();
            
            // Find the connection in the cache and remove it
            // dynamic is used to access the connectionId property in the object
            // and compare it with the current connectionId to remove it
            var updatedConnections = connections.Where(c =>
            {
                dynamic connection = c;
                return connection.connectionId != Context.ConnectionId;
            }).ToList();
            _cache.Set("connections", updatedConnections);
            
            Console.WriteLine($"Connection {Context.ConnectionId} removed from cache.");
        }
        else
        {
            Console.WriteLine($"Warning: Connection {Context.ConnectionId} could not be cleared.");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SwitchUser(int userId)
    {
        var currentUser = await _userRepository.GetUserByConnectionIdAsync(Context.ConnectionId);
        if (currentUser == null)
        {
            Console.WriteLine("No user associated with this connection.");
            await Clients.Caller.SendAsync("SwitchUserFailed", "No active user found.");
            return;
        }

        var availableUser = (await _userRepository.GetAllUsersAsync())
            .FirstOrDefault(u => string.IsNullOrEmpty(u.ConnectionId));

        if (availableUser == null)
        {
            await Clients.Caller.SendAsync("SwitchUserFailed", "No available user slots.");
            return;
        }
        await _userRepository.AssignConnectionAsync(userId, null);
        // Assign the new user to this connection
        await _userRepository.AssignConnectionAsync(availableUser.Id, Context.ConnectionId);

        await Clients.Caller.SendAsync("UserAssigned", availableUser.Id);
    }

    public async Task UpdateVisibleCells(int userId, List<int> rows, List<int> cols)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[UpdateVisibleCells] UserId={userId}, ConnectionId={connectionId}");

        // Add or update the user-visible cells in the cache
        var userVisibleCells = _cache.Get<List<UserVisibleCells>>("userVisibleCells") ?? new List<UserVisibleCells>();

        var existingUser = userVisibleCells.FirstOrDefault(u => u.UserId == userId);
        if (existingUser != null)
        {
            existingUser.Rows = rows;
            existingUser.Cols = cols;
        }
        else
        {
            userVisibleCells.Add(new UserVisibleCells
            {
                UserId = userId,
                ConnectionId = connectionId,
                Rows = rows,
                Cols = cols
            });
        }
        Console.WriteLine($"Updated visible cells for UserId={userId}: Rows={string.Join(",", rows)}, Cols={string.Join(",", cols)}");
        _cache.Set("userVisibleCells", userVisibleCells);
        var grid = await _gridRepository.GetGridAsync(1); // Assuming gridId = 1 for now
        if (grid == null) throw new Exception($"Grid with ID {1} not found");

        var response = new
        {
            GridData = Convert.ToBase64String(grid.GridData), // Send as Base64
            RevealedData = Convert.ToBase64String(grid.RevealedData) // Send as Base64
        };
        
        var connections = _cache.Get<List<object>>("connections") ?? new List<object>();
        connections.Add(new { connectionId = connectionId, rows = rows, cols = cols });
        _cache.Set("connections", connections);    

        await Clients.Caller.SendAsync("GridData", response);
        Console.WriteLine($"Updated visible cells for UserId={userId}: Rows={string.Join(",", rows)}, Cols={string.Join(",", cols)}");
    }

        public async Task RevealAll()
        {
            try
            {
                Console.WriteLine("Revealing all cells...");

                var gridId = 1;
                var grid = await _gridRepository.GetGridAsync(gridId);

                if (grid == null)
                {
                    Console.WriteLine($"Grid with ID {gridId} not found.");
                    return;
                }

                // Mark all cells as revealed
                for (int i = 0; i < grid.RevealedData.Length; i++)
                {
                    grid.RevealedData[i] = 0xFF; // All bits set to 1
                }

                // Save the updated grid to the database
                await _gridRepository.UpdateGridAsync(grid);

                await Clients.All.SendAsync("GridData", new
                {
                    GridData = Convert.ToBase64String(grid.GridData), // Send as Base64
                    RevealedData = Convert.ToBase64String(grid.RevealedData) // Send as Base64
                });

                Console.WriteLine("All cells have been revealed and clients notified.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RevealAll: {ex.Message}");
                throw;
            }
        }
        public async Task ResetGrid()
        {
            Console.WriteLine("Resetting grid...");
            var grid = await _gridRepository.ResetGridAsync(1);
            
            await _userRepository.ResetScratchCountsAsync();
            await Clients.All.SendAsync("GridReset", new {
                GridData = Convert.ToBase64String(grid.GridData), // Send as Base64
                RevealedData = Convert.ToBase64String(grid.RevealedData) // Send as Base64
            }
            );
        }
        
        public async Task CellClicked(int userId, int row, int col)
        {
            try
            {
                Console.WriteLine($"[CellClicked] UserId={userId}, Row={row}, Col={col}");

                var gridId = 1; 
                var grid = await _gridRepository.GetGridAsync(gridId);

                int cellIndex = row * 100 + col; //100x100
                int byteIndex = cellIndex / 8;
                int bitOffset = cellIndex % 8;

                // Check if the cell is already revealed
                // this is done by checking if the bit at the offset is 1
                if ((grid.RevealedData[byteIndex] & (1 << bitOffset)) != 0)
                {
                    Console.WriteLine($"Cell {row},{col} is already revealed.");
                    await Clients.Caller.SendAsync("CellAlreadyRevealed", row, col);
                    return;
                }

                var user = await _userRepository.GetUserByConnectionIdAsync(Context.ConnectionId);
                if (user.ScratchCount >= 1)
                {
                    Console.WriteLine($"User {user.Id} has no scratches left.");
                    await Clients.Caller.SendAsync("NoScratchesLeft");
                    return;
                }

                // Extract the state of the cell
                int stateByteIndex = cellIndex / 4; // Each byte holds 4 cells (2 bits per cell)
                int stateBitOffset = (cellIndex % 4) * 2;
                // Extract the 2-bit state of the cell (00, 01, 10, 11) 
                // this is done by shifting the bits to the right by the offset and then masking with 0b11
                var cellValue = (grid.GridData[stateByteIndex] >> stateBitOffset) & 0b11;

                // Mark the cell as revealed
                // this is done by setting the bit at the offset to 1
                grid.RevealedData[byteIndex] |= (byte)(1 << bitOffset);
                await _gridRepository.UpdateGridAsync(grid);
                // Increment the user's scratch count
                await _userRepository.IncrementScratchCountAsync(user.Id);
                // Get all user-visible ranges from the cache
                var userVisibleCells = _cache.Get<List<UserVisibleCells>>("userVisibleCells") ?? new List<UserVisibleCells>();
                Console.WriteLine($"UserVisibleCells: {string.Join(", ", userVisibleCells.Select(u => u.UserId))}");
                // Notify relevant users whose visible ranges include the clicked cell
                foreach (var userVisible in userVisibleCells)
                {
                    if (userVisible.Rows.Contains(row) && userVisible.Cols.Contains(col))
                    {
                        Console.WriteLine("Current UserVisibleCells:");
                        // Print the current user-visible cells
                        foreach (var a in userVisibleCells)
                        {
                            Console.WriteLine($"UserId={a.UserId}, ConnectionId={a.ConnectionId}, Rows={string.Join(",", a.Rows)}, Cols={string.Join(",", a.Cols)}");
                        }
                        // debug end

                        // Notify the user about the revealed cell
                        Console.WriteLine($"Notifying UserId={userVisible.UserId} at ConnectionId={userVisible.ConnectionId} about revealed cell at Row={row}, Col={col}, Value={cellValue}");
                        await Clients.Client(userVisible.ConnectionId).SendAsync("CellRevealed", row, col, cellValue);
                       
                        if (!userVisibleCells.Any(u => u.ConnectionId == Context.ConnectionId))
                        {
                            await Clients.Caller.SendAsync("CellRevealed", row, col, cellValue);
                        }
                    }
                }

                // Notify the caller about the revealed cell
                await Clients.Caller.SendAsync("CellRevealed", row, col, cellValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CellClicked: {ex.Message}");
                throw;
            }
        }

    }
}
