using figma_backend.Database;
using figma_backend.Entities;
using figma_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace figma_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomModel createRoomDto)
        {
            // Verificar si el usuario existe
            var user = await _context.Users.FindAsync(createRoomDto.CreatorUserId);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Crear la nueva sala
            var newRoom = new CanvasRoom
            {
                Name = createRoomDto.Name,
                CreatedAt = DateTime.UtcNow,
                CreatorUserId = createRoomDto.CreatorUserId
            };

            _context.CanvasRooms.Add(newRoom);
            await _context.SaveChangesAsync();

            // Registrar al creador como usuario conectado
            // (En este punto solo creamos la sala, la conexión real se hará via SignalR)

            return Ok(new
            {
                RoomId = newRoom.Id,
                newRoom.Name,
                CreatedAt = newRoom.CreatedAt,
                Creator = new
                {
                    user.UserId,
                    user.Username
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRooms(int userId)
        {
            var rooms = await _context.CanvasRooms
                .Where(r => r.CreatorUserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.CreatedAt,
                    ComponentCount = r.Components.Count,
                    UserCount = r.ConnectedUsers.Count
                })
                .ToListAsync();

            return Ok(rooms);
        }

        [HttpGet("GetAssignedUserRooms/{userId}")]
        public async Task<IActionResult> GetAssignedUserRooms(int userId)
        {
            var assignedUserRooms = await _context.RoomUsers
                .Where(ru => ru.UserId == userId)
                .Include(ru => ru.User)
                .Select(ru => new
                {
                    ru.User.UserId,
                    ru.User.Username,
                    ru.User.Email,
                    ru.ConnectionId,
                    ru.RoomId
                })
                .ToListAsync();

            return Ok(assignedUserRooms);
        }

        [HttpGet("{roomId}/users")]
        public async Task<IActionResult> GetRoomUsers(int roomId)
        {
            var users = await _context.RoomUsers
                .Where(ru => ru.RoomId == roomId)
                .Include(ru => ru.User)
                .Select(ru => new
                {
                    ru.User.UserId,
                    ru.User.Username,
                    ru.User.Email,
                    ru.ConnectionId
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("{roomId}/users")]
        public async Task<IActionResult> AddUserToRoom(int roomId, [FromBody] AddUserToRoomDto dto)
        {
            var room = await _context.CanvasRooms.FindAsync(roomId);
            if (room == null)
                return NotFound("Sala no encontrada");

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound("Usuario no encontrado");

            // Verificar si el usuario ya está en la sala
            var existingUser = await _context.RoomUsers
                .FirstOrDefaultAsync(ru => ru.RoomId == roomId && ru.UserId == dto.UserId);

            if (existingUser != null)
                return BadRequest("El usuario ya está en esta sala");

            // En una implementación real, aquí habría lógica de invitación/permisos

            var roomUser = new RoomUser
            {
                RoomId = roomId,
                UserId = dto.UserId,
                UserName = user.Username,
                ConnectionId = dto.ConnectionId ?? "offline" // Temporal hasta conexión SignalR
            };

            _context.RoomUsers.Add(roomUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                roomUser.UserId,
                roomUser.UserName,
                roomUser.RoomId
            });
        }
    }
}
