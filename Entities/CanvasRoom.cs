namespace figma_backend.Entities
{
    public class CanvasRoom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatorUserId { get; set; }
        public User CreatorUser { get; set; }

        public List<CanvasComponent> Components { get; set; } = new();
        public List<RoomUser> ConnectedUsers { get; set; } = new();
    }
}
