using System.ComponentModel.DataAnnotations;

namespace figma_backend.Models
{
    public class AddUserToRoomDto
    {
        [Required]
        public int UserId { get; set; }

        public string? ConnectionId { get; set; } // Para SignalR
    }
}
