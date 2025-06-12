namespace figma_backend.Entities
{
    public class CanvasComponent
    {
        public string Id { get; set; }
        public string Type { get; set; } // "button", "text", "image", "input"
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? Color { get; set; }
        public string? Content { get; set; }
        public int RoomId { get; set; }
    }
}
