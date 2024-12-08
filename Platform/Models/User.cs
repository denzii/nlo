using System.ComponentModel.DataAnnotations;

namespace Platform.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string? ConnectionId { get; set; }
        public int ScratchCount { get; set; } = 0;
    }
}
