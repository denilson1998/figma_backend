namespace figma_backend.Entities
{
    public class RoomUser
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public int RoomId { get; set; }

        public CanvasRoom Room { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
