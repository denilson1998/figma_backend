namespace figma_backend.Entities
{
    public class RoomUser
    {
        public int RoomId { get; set; }

        public int UserId { get; set; }

        public string ConnectionId { get; set; }
        
        public string UserName { get; set; }
        public User User { get; set; }
        public CanvasRoom Room { get; set; }
    }
}
