using Platform.Models;

namespace Platform.Repositories
{
    public interface IGridRepository
    {
        Task<GridCell> GetGridAsync(int id);
        Task<GridCell> ResetGridAsync(int id);
        Task UpdateGridAsync(GridCell grid);
    }
}
