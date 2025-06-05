using figma_backend.Database;
using figma_backend.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace figma_backend.Hubs
{
    public class CanvasHub : Hub
    {
        
        private static readonly Dictionary<int, CanvasRoom> _rooms = new();

        private ApplicationDbContext _dbContext;

        public CanvasHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task JoinRoom(int roomId, int userId)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    room = new CanvasRoom { Id = roomId, Name = "New Room" };
                    _rooms.Add(roomId, room);
                }

                var userData = await _dbContext.Users.Where(c => c.UserId == userId).FirstOrDefaultAsync();

                var user = new RoomUser
                {
                    ConnectionId = Context.ConnectionId,
                    UserName = userData.Username,
                    RoomId = roomId
                };

                room.ConnectedUsers.Add(user);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

                await Clients.Caller.SendAsync("RoomState", room);

                await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserJoined", userData.Username);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task AddComponent(int roomId, CanvasComponent component)
        {
            try
            {
                if (_rooms.TryGetValue(roomId, out var room))
                {
                    room.Components.Add(component);
                    await Clients.Group(roomId.ToString()).SendAsync("ComponentAdded", component);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AddComponent: {ex.Message}");
                throw;
            }
        }

        public async Task MoveComponent(int roomId, string componentId, int newX, int newY)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var component = room.Components.FirstOrDefault(c => c.Id == componentId);
                if (component != null)
                {
                    component.PositionX = newX;
                    component.PositionY = newY;
                    await Clients.OthersInGroup(roomId.ToString()).SendAsync("ComponentMoved", componentId, newX, newY);
                }
            }
        }

        public async Task RemoveComponent(int roomId, string componentId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var component = room.Components.FirstOrDefault(c => c.Id == componentId);
                if (component != null)
                {
                    room.Components.Remove(component);
                    await Clients.Group(roomId.ToString()).SendAsync("ComponentRemoved", componentId);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Limpieza cuando un usuario se desconecta
            var user = _rooms.Values
                .SelectMany(r => r.ConnectedUsers)
                .FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                if (_rooms.TryGetValue(user.RoomId, out var room))
                {
                    room.ConnectedUsers.RemoveAll(u => u.ConnectionId == Context.ConnectionId);
                    await Clients.Group(user.RoomId.ToString())
                        .SendAsync("UserLeft", user.UserName);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task ChangeDeviceSize(int roomId, string name, int width, int height)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var user = room.ConnectedUsers.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
                if (user != null)
                {
                    await Clients.OthersInGroup(roomId.ToString()).SendAsync("DeviceSizeChanged", user.UserName, name, width, height);
                }
            }
        }
    }
}
