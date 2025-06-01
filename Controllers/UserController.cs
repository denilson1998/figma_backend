using figma_backend.Database;
using figma_backend.Entities;
using figma_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace figma_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterModel dto)
        {

            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(dto.Username))
            {
                return BadRequest("Datos invalidos!");
            }

            if (_context.Set<User>().Any(u => u.Username == dto.Username))
            {
                return BadRequest("El nombre de usuario ya existe.");
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = dto.Password,
                Email = dto.Email   
            };

            _context.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "Usuario registrado con éxito." });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginModel dto)
        {
            var user = _context.Set<User>().FirstOrDefault(u =>
                u.Email == dto.Email && u.PasswordHash == dto.Password);

            if (user == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(new { message = "Login exitoso." , data = $"{user.UserId}"});
        }

        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var user = _context.Set<User>().ToList();

            if (user == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(new { message = "Usuarios obtenidos.", data = user });
        }


    }
}
