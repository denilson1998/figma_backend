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
            //if (_rooms.TryGetValue(roomId, out var room))
            //{
            //    var component = room.Components.FirstOrDefault(c => c.Id == componentId);
            //    if (component != null)
            //    {
            //        component.PositionX = newX;
            //        component.PositionY = newY;
            //        await Clients.OthersInGroup(roomId.ToString()).SendAsync("ComponentMoved", componentId, newX, newY);
            //    }
            //}
            try
            {
                if (roomId <= 0 || string.IsNullOrEmpty(componentId))
                {
                    throw new ArgumentException("Invalid roomId or componentId");
                }

                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    throw new KeyNotFoundException($"Room {roomId} not found");
                }

                var component = room.Components.FirstOrDefault(c => c.Id == componentId);
                if (component == null)
                {
                    throw new KeyNotFoundException($"Component {componentId} not found in room {roomId}");
                }

                // Validación adicional para coordenadas (ajusta los límites según tu caso)
                if (newX < 0 || newY < 0 || newX > 5000 || newY > 5000)
                {
                    throw new ArgumentOutOfRangeException($"Coordinates ({newX}, {newY}) are out of valid range");
                }

                // Actualiza las coordenadas
                component.PositionX = newX;
                component.PositionY = newY;

                // Notifica a todos los clientes en la sala (excepto al que envió el movimiento)
                await Clients.OthersInGroup(roomId.ToString())
                    .SendAsync("ComponentMoved", componentId, newX, newY);
            }
            catch (Exception ex)
            {
                // Log del error (puedes usar ILogger si está disponible)
                Console.WriteLine($"Error in MoveComponent: {ex.Message}");
                throw; // Re-lanza la excepción para que SignalR la maneje
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

        public async Task UpdateComponent(int roomId, CanvasComponent updatedComponent)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var existingComponent = room.Components.FirstOrDefault(c => c.Id == updatedComponent.Id);
                if (existingComponent != null)
                {
                    // Actualiza todas las propiedades
                    existingComponent.PositionX = updatedComponent.PositionX;
                    existingComponent.PositionY = updatedComponent.PositionY;
                    existingComponent.Width = updatedComponent.Width;
                    existingComponent.Height = updatedComponent.Height;
                    //existingComponent.FlutterCode = updatedComponent.FlutterCode;
                    // ... otras propiedades

                    await Clients.Group(roomId.ToString())
                        .SendAsync("ComponentUpdated", updatedComponent);
                }
            }
        }
    }
}
