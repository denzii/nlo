using Microsoft.EntityFrameworkCore;
using Platform.Models;
using static Platform.Models.GridCell;

namespace Platform.Data
{
    public class GridDbContext : DbContext
    {
        public DbSet<GridCell> GridCells { get; set; }
        public DbSet<User> Users { get; set; }
        
        public GridDbContext(DbContextOptions<GridDbContext> options) : base(options) { }

    async protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var gridCell = new GridCell
        {
            Id = 1,
            GridData = GridCell.GenerateRandom(),
            RevealedData = new byte[2500]
        };

        await Console.Out.WriteLineAsync("Seeding GridCell entity...");
        DebugPrizeCounts(gridCell.GridData);
        Console.Out.Flush();
        try{
            modelBuilder.Entity<GridCell>().HasData(gridCell);
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, ConnectionId = null, ScratchCount = 0 },
                new User { Id = 2, ConnectionId = null, ScratchCount = 0 },
                new User { Id = 3, ConnectionId = null, ScratchCount = 0 },
                new User { Id = 4, ConnectionId = null, ScratchCount = 0 }
            );
        }
        catch(Exception e){
            await Console.Out.WriteLineAsync(e.ToString());
        }
    }



    async private void DebugPrizeCounts(byte[] gridData)
    {
        int grandPrizeCount = 0;
        int consolationPrizeCount = 0;

        for (int i = 0; i < 10000; i++)
        {
            // Calculate the byte index and bit offset for the cell index
            // Each cell is represented by 2 bits in the byte array
            int byteIndex = i / 4;
            int bitOffset = (i % 4) * 2;

            // Extract the 2-bit value for the cell
            // This is done by shifting the bits to the right by the bit offset
            // and masking the result with 0b11 (3 in decimal) to get the 2 least significant bits
            //  0b00 -> Empty cell
            //  0b01 -> Grand prize
            //  0b10 -> Consolation prize
            byte cellValue = (byte)((gridData[byteIndex] >> bitOffset) & 0b11);
            if (cellValue == 0b01) grandPrizeCount++;
            if (cellValue == 0b10) consolationPrizeCount++;
        }

        Console.Out.WriteLine($"Grand prizes to seed: {grandPrizeCount}, Consolation prizes to seed: {consolationPrizeCount}");
    }

    }
    
}
