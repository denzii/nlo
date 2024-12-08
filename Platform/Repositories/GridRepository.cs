using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;
using static Platform.Models.GridCell;

namespace Platform.Repositories
{
    public class GridRepository : IGridRepository
    {
        private readonly GridDbContext _context;

        public GridRepository(GridDbContext context)
        {
            _context = context;
        }

        public async Task<GridCell> GetGridAsync(int id)
        {
            return await _context.GridCells.FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<GridCell> ResetGridAsync(int id)
        {
            var gridCell = await GetGridAsync(id) ?? new GridCell { Id = id };

            gridCell.GridData = GridCell.GenerateRandom();
            gridCell.RevealedData = new byte[2500]; 

            if (_context.GridCells.Any(g => g.Id == id))
            {
                _context.GridCells.Update(gridCell);
            }
            else
            {
                _context.GridCells.Add(gridCell);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return gridCell;
        }

        public async Task UpdateGridAsync(GridCell grid)
        {
            _context.GridCells.Update(grid);
            await _context.SaveChangesAsync();
        }
    }
}
