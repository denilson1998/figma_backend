using figma_backend.Entities;
using Microsoft.AspNetCore.SignalR;

namespace figma_backend.Hubs
{
    public class CanvasHub : Hub
    {
        
        private static readonly Dictionary<int, CanvasRoom> _rooms = new();

        public async Task JoinRoom(int roomId, string userName)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                room = new CanvasRoom { Id = roomId, Name = "New Room" };
                _rooms.Add(roomId, room);
            }

            var user = new RoomUser
            {
                ConnectionId = Context.ConnectionId,
                UserName = userName,
                RoomId = roomId
            };

            room.ConnectedUsers.Add(user);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

            // Enviar estado actual del canvas al nuevo usuario
            await Clients.Caller.SendAsync("RoomState", room);

            // Notificar a otros usuarios
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserJoined", userName);
        }

        public async Task AddComponent(int roomId, CanvasComponent component)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.Components.Add(component);
                await Clients.Group(roomId.ToString()).SendAsync("ComponentAdded", component);
            }
        }

        public async Task MoveComponent(int roomId, int componentId, int newX, int newY)
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

        public async Task RemoveComponent(int roomId, int componentId)
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
    }
}
