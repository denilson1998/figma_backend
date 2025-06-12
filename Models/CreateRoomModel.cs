using System.ComponentModel.DataAnnotations;

namespace figma_backend.Models
{
    public class CreateRoomModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public int CreatorUserId { get; set; }
    }
}
