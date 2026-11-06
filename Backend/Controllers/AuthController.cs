using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static readonly ConcurrentDictionary<int, DateTime> ActiveSessionPings = new ConcurrentDictionary<int, DateTime>();
        
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register-equipment")]
        public async Task<IActionResult> RegisterEquipment([FromBody] RegisterEquipmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreRed))
            {
                return BadRequest(new { message = "El nombre de red es requerido." });
            }

            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == request.NombreRed);
            if (equipo == null)
            {
                equipo = new Equipo { NombreRed = request.NombreRed, Ubicacion = "Laboratorio Central", Estado = true };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Equipo registrado." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var codigo = request.CodigoUniversitario?.Trim() ?? string.Empty;
            var dni = request.DNI?.Trim() ?? string.Empty;
            var nombreRed = request.NombreRed?.Trim() ?? string.Empty;

            request.CodigoUniversitario = codigo;
            request.DNI = dni;
            request.NombreRed = nombreRed;

            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.CodigoUniversitario.Trim() == codigo && a.DNI.Trim() == dni);

            if (alumno == null)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            if (!alumno.Estado)
            {
                return Unauthorized(new { message = "El alumno se encuentra inactivo." });
            }

            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.NombreRed == request.NombreRed);

            if (equipo == null)
            {
                equipo = new Equipo { NombreRed = request.NombreRed, Ubicacion = "Laboratorio Central", Estado = true };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();
            }

            if (!equipo.Estado)
            {
                return BadRequest(new { message = "Este equipo está fuera de servicio." });
            }

            var sesionActiva = await _context.Sesiones
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);

            if (sesionActiva != null)
            {
                if (sesionActiva.AlumnoID == alumno.AlumnoID && (TimeHelper.GetPeruTime() - sesionActiva.HoraInicio).TotalMinutes < 3)
                {
                    ActiveSessionPings[sesionActiva.SesionID] = TimeHelper.GetPeruTime();
                    double remSeconds = 0;
                    if (sesionActiva.HoraLimite.HasValue)
                    {
                        remSeconds = (sesionActiva.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                        if (remSeconds < 0) remSeconds = 0;
                    }

                    return Ok(new
                    {
                        sesionId = sesionActiva.SesionID,
                        horaLimite = sesionActiva.HoraLimite,
                        remainingSeconds = remSeconds,
                        alumno = new
                        {
                            nombres = alumno.Nombres,
                            apellidos = $"{alumno.ApellidoPaterno} {alumno.ApellidoMaterno}"
                        }
                    });
                }

                sesionActiva.HoraFin = TimeHelper.GetPeruTime();
                
                if (sesionActiva.Fecha.Date < TimeHelper.GetPeruTime().Date)
                {
                    sesionActiva.HoraFin = sesionActiva.HoraLimite ?? sesionActiva.HoraInicio.AddHours(3);
                }
                
                await _context.SaveChangesAsync();
            }

            var sesionAlumnoActiva = await _context.Sesiones
                .Include(s => s.Equipo)
                .FirstOrDefaultAsync(s => s.AlumnoID == alumno.AlumnoID && s.HoraFin == null);

            if (sesionAlumnoActiva != null)
            {
                var ahora = TimeHelper.GetPeruTime();

                if (sesionAlumnoActiva.Fecha.Date < ahora.Date)
                {
                    sesionAlumnoActiva.HoraFin = sesionAlumnoActiva.HoraLimite ?? sesionAlumnoActiva.HoraInicio.AddHours(3);
                }
                else
                {
                    sesionAlumnoActiva.HoraFin = ahora;
                }

                ActiveSessionPings.TryRemove(sesionAlumnoActiva.SesionID, out _);

                await _context.SaveChangesAsync();
            }

            var sesionesHoy = await _context.Sesiones
                .Where(s => s.AlumnoID == alumno.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                .ToListAsync();

            if (sesionesHoy.Count == 0 && alumno.LimiteDiarioSegundos != 10800)
            {
                alumno.LimiteDiarioSegundos = 10800;
                await _context.SaveChangesAsync();
            }

            double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
            double segundosLimite = alumno.LimiteDiarioSegundos;
            double segundosRestantes;

            if (segundosConsumidosHoy >= segundosLimite)
            {
                if (alumno.Estado)
                {
                    if (alumno.LimiteDiarioSegundos <= segundosConsumidosHoy)
                    {
                        alumno.LimiteDiarioSegundos = (int)segundosConsumidosHoy + 10800;
                        await _context.SaveChangesAsync();
                        segundosLimite = alumno.LimiteDiarioSegundos;
                    }
                    segundosRestantes = segundosLimite - segundosConsumidosHoy;
                    if (segundosRestantes <= 0) segundosRestantes = 10800;
                }
                else
                {
                    alumno.Estado = false;
                    await _context.SaveChangesAsync();
                    return Unauthorized(new { message = $"Has consumido tu límite de {(alumno.LimiteDiarioSegundos / 3600)} horas por el día de hoy." });
                }
            }
            else
            {
                double tiempoRestante = segundosLimite - segundosConsumidosHoy;

                bool tieneSessionesPreviasHoy = sesionesHoy.Count > 0;
                bool tiempoRestanteAnomalo = tiempoRestante < (15 * 60);

                if (tiempoRestanteAnomalo && tieneSessionesPreviasHoy && alumno.Estado)
                {
                    alumno.LimiteDiarioSegundos = (alumno.LimiteDiarioSegundos < 10800 ? 10800 : alumno.LimiteDiarioSegundos) + 10800;
                    await _context.SaveChangesAsync();
                    segundosRestantes = alumno.LimiteDiarioSegundos - segundosConsumidosHoy;
                }
                else
                {
                    segundosRestantes = tiempoRestante > 0 ? tiempoRestante : segundosLimite;
                }
            }

            var nuevaSesion = new Sesion
            {
                AlumnoID = alumno.AlumnoID,
                EquipoID = equipo.EquipoID,
                Fecha = TimeHelper.GetPeruTime().Date,
                HoraInicio = TimeHelper.GetPeruTime(),
                HoraLimite = TimeHelper.GetPeruTime().AddSeconds(segundosRestantes)
            };

            _context.Sesiones.Add(nuevaSesion);
            await _context.SaveChangesAsync();

            ActiveSessionPings[nuevaSesion.SesionID] = TimeHelper.GetPeruTime();

            return Ok(new
            {
                sesionId = nuevaSesion.SesionID,
                horaLimite = nuevaSesion.HoraLimite,
                remainingSeconds = segundosRestantes,
                alumno = new
                {
                    nombres = alumno.Nombres,
                    apellidos = $"{alumno.ApellidoPaterno} {alumno.ApellidoMaterno}"
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var sesion = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.SesionID == request.SesionId);

            if (sesion == null)
            {
                return NotFound(new { message = "Sesión no encontrada." });
            }

            sesion.HoraFin = TimeHelper.GetPeruTime();
            await _context.SaveChangesAsync();
            
            ActiveSessionPings.TryRemove(request.SesionId, out _);

            if (sesion.Alumno != null)
            {
                var sesionesHoy = await _context.Sesiones
                    .Where(s => s.AlumnoID == sesion.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                    .ToListAsync();

                double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                
                if (segundosConsumidosHoy >= (sesion.Alumno.LimiteDiarioSegundos - 60))
                {
                    sesion.Alumno.Estado = false;
                }
                else
                {
                    sesion.Alumno.Estado = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Sesión finalizada." });
        }

        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            var username = request.Username?.Trim() ?? string.Empty;
            var password = request.Password?.Trim() ?? string.Empty;

            var user = await _context.UsuariosAdmin.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user != null)
            {
                if (!user.EstadoActivo)
                {
                    return Unauthorized(new { message = "Usuario inactivo o suspendido." });
                }

                bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                if (!validPassword)
                {
                    return Unauthorized(new { message = "Credenciales incorrectas." });
                }

                user.UltimoAcceso = DateTime.UtcNow;

                var token = GenerateJwtToken(user.IdUsuarioAdmin.ToString(), user.Username, user.Rol.ToString());
                var refreshTokenString = GenerateRefreshTokenString();

                var refreshTokenEntity = new RefreshToken
                {
                    IdUsuarioAdmin = user.IdUsuarioAdmin,
                    Token = refreshTokenString,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return Ok(new AdminLoginResponse
                {
                    Token = token,
                    RefreshToken = refreshTokenString,
                    EsLegacy = false,
                    Usuario = new UsuarioAdminDto
                    {
                        IdUsuarioAdmin = user.IdUsuarioAdmin,
                        Username = user.Username,
                        Nombres = user.Nombres,
                        Correo = user.Correo,
                        Rol = user.Rol,
                        RolNombre = user.Rol.ToString(),
                        EstadoActivo = user.EstadoActivo,
                        FechaCreacion = user.FechaCreacion,
                        UltimoAcceso = user.UltimoAcceso
                    }
                });
            }

            bool allowLegacy = _configuration.GetValue<bool>("AdminSettings:AllowLegacyAdmin", true);
            var expectedUsername = _configuration["AdminSettings:Username"] ?? "admin";
            var expectedPassword = _configuration["AdminSettings:Password"] ?? "admin123";

            if (allowLegacy && username == expectedUsername && password == expectedPassword)
            {
                var token = GenerateJwtToken("0", expectedUsername, RolAdmin.SuperAdmin.ToString());
                var refreshTokenString = GenerateRefreshTokenString();

                return Ok(new AdminLoginResponse
                {
                    Token = token,
                    RefreshToken = refreshTokenString,
                    EsLegacy = true,
                    Usuario = new UsuarioAdminDto
                    {
                        IdUsuarioAdmin = 0,
                        Username = expectedUsername,
                        Nombres = "Administrador Legacy",
                        Correo = "admin@urp.edu.pe",
                        Rol = RolAdmin.SuperAdmin,
                        RolNombre = RolAdmin.SuperAdmin.ToString(),
                        EstadoActivo = true,
                        FechaCreacion = DateTime.UtcNow,
                        UltimoAcceso = DateTime.UtcNow
                    }
                });
            }

            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Token de refresco requerido." });
            }

            var existingToken = await _context.RefreshTokens
                .Include(t => t.UsuarioAdmin)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (existingToken == null || existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Token de refresco inválido o expirado." });
            }

            if (existingToken.UsuarioAdmin == null || !existingToken.UsuarioAdmin.EstadoActivo)
            {
                return Unauthorized(new { message = "El usuario se encuentra inactivo." });
            }

            var newRefreshTokenString = GenerateRefreshTokenString();
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByToken = newRefreshTokenString;

            var newJwt = GenerateJwtToken(existingToken.UsuarioAdmin.IdUsuarioAdmin.ToString(), existingToken.UsuarioAdmin.Username, existingToken.UsuarioAdmin.Rol.ToString());

            var newRefreshTokenEntity = new RefreshToken
            {
                IdUsuarioAdmin = existingToken.IdUsuarioAdmin,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            return Ok(new AdminLoginResponse
            {
                Token = newJwt,
                RefreshToken = newRefreshTokenString,
                EsLegacy = false,
                Usuario = new UsuarioAdminDto
                {
                    IdUsuarioAdmin = existingToken.UsuarioAdmin.IdUsuarioAdmin,
                    Username = existingToken.UsuarioAdmin.Username,
                    Nombres = existingToken.UsuarioAdmin.Nombres,
                    Correo = existingToken.UsuarioAdmin.Correo,
                    Rol = existingToken.UsuarioAdmin.Rol,
                    RolNombre = existingToken.UsuarioAdmin.Rol.ToString(),
                    EstadoActivo = existingToken.UsuarioAdmin.EstadoActivo,
                    FechaCreacion = existingToken.UsuarioAdmin.FechaCreacion,
                    UltimoAcceso = existingToken.UsuarioAdmin.UltimoAcceso
                }
            });
        }

        [HttpPost("admin-logout")]
        public async Task<IActionResult> AdminLogout([FromBody] RefreshTokenRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
                if (existingToken != null && existingToken.RevokedAt == null)
                {
                    existingToken.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Sesión administrativa cerrada." });
        }

        private string GenerateJwtToken(string sub, string name, string role)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "FamurpBve2026_ClaveSecretaSuperSeguraParaJwt_Produccion!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim("name", name),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshTokenString()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }


        [HttpGet("session-status/{sesionId}")]
        public async Task<IActionResult> GetSessionStatus(int sesionId)
        {
            var sesion = await _context.Sesiones.FindAsync(sesionId);
            if (sesion == null) return NotFound();
            
            ActiveSessionPings[sesionId] = TimeHelper.GetPeruTime();
            
            double remainingSeconds = 0;
            if (sesion.HoraLimite.HasValue)
            {
                remainingSeconds = (sesion.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                if (remainingSeconds < 0) remainingSeconds = 0;
            }

            return Ok(new { horaLimite = sesion.HoraLimite, isFinished = sesion.HoraFin != null, remainingSeconds = remainingSeconds });
        }

        [HttpPost("extend-session")]
        public async Task<IActionResult> ExtendSession([FromBody] ExtendSessionRequest request)
        {
            var sesion = await _context.Sesiones.Include(s => s.Alumno).FirstOrDefaultAsync(s => s.SesionID == request.SesionId);
            if (sesion == null) return NotFound(new { message = "Sesión no encontrada." });
            if (sesion.HoraFin != null) return BadRequest(new { message = "La sesión ya ha finalizado." });
            if (sesion.Alumno == null) return BadRequest(new { message = "Alumno no encontrado." });

            sesion.Alumno.LimiteDiarioSegundos += 10800;

            sesion.HoraLimite = (sesion.HoraLimite ?? TimeHelper.GetPeruTime()).AddHours(3);

            await _context.SaveChangesAsync();

            double newRemaining = (sesion.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
            if (newRemaining < 0) newRemaining = 0;

            return Ok(new { message = "Sesión extendida exitosamente.", remainingSeconds = newRemaining, horaLimite = sesion.HoraLimite });
        }

        [HttpPost("set-limit")]
        public async Task<IActionResult> SetSessionLimit([FromBody] SetSessionLimitRequest request)
        {
            var sesion = await _context.Sesiones.FindAsync(request.SesionId);
            if (sesion == null) return NotFound(new { message = "Sesión no encontrada." });
            if (sesion.HoraFin != null) return BadRequest(new { message = "La sesión ya ha finalizado." });

            sesion.HoraLimite = request.NuevaHoraLimite;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tiempo actualizado.", horaLimite = sesion.HoraLimite });
        }

        [HttpGet("active-session/{nombreRed}")]
        public async Task<IActionResult> GetActiveSession(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            var sesionActiva = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);

            if (sesionActiva == null) return Ok(new { hasActiveSession = false });

            double remainingSeconds = 0;
            if (sesionActiva.HoraLimite.HasValue)
            {
                remainingSeconds = (sesionActiva.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                if (remainingSeconds < 0) remainingSeconds = 0;
            }

            return Ok(new
            {
                hasActiveSession = true,
                sesionId = sesionActiva.SesionID,
                horaLimite = sesionActiva.HoraLimite,
                remainingSeconds = remainingSeconds,
                alumno = new
                {
                    nombres = sesionActiva.Alumno?.Nombres,
                    apellidos = $"{sesionActiva.Alumno?.ApellidoPaterno} {sesionActiva.Alumno?.ApellidoMaterno}"
                }
            });
        }

        private static readonly ConcurrentDictionary<string, int> PendingUnlocks = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, DateTime> PendingShutdowns = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        [HttpPost("trigger-remote-unlock/{nombreRed}")]
        public async Task<IActionResult> TriggerRemoteUnlock(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            var admin = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == "ADMIN");
            if (admin == null)
            {
                admin = new Alumno 
                { 
                    CodigoUniversitario = "ADMIN", 
                    DNI = "00000000", 
                    Nombres = "ADMINISTRADOR", 
                    ApellidoPaterno = "SISTEMA",
                    ApellidoMaterno = "BVE",
                    CorreoInstitucional = "admin@bve.com",
                    Carrera = "SOPORTE"
                };
                _context.Alumnos.Add(admin);
                await _context.SaveChangesAsync();
            }

            var sesion = new Sesion
            {
                AlumnoID = admin.AlumnoID,
                EquipoID = equipo.EquipoID,
                Fecha = TimeHelper.GetPeruTime().Date,
                HoraInicio = TimeHelper.GetPeruTime(),
                HoraLimite = TimeHelper.GetPeruTime().AddHours(5)
            };
            _context.Sesiones.Add(sesion);
            await _context.SaveChangesAsync();

            PendingUnlocks[nombreRed] = sesion.SesionID;
            return Ok(new { message = "Desbloqueo enviado al equipo.", sesionId = sesion.SesionID });
        }

        [HttpPost("trigger-remote-unlock-all")]
        public async Task<IActionResult> TriggerRemoteUnlockAll([FromQuery] bool onlyFree = false)
        {
            var equipos = await _context.Equipos.ToListAsync();
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

            var admin = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == "ADMIN");
            if (admin == null)
            {
                admin = new Alumno 
                { 
                    CodigoUniversitario = "ADMIN", 
                    DNI = "00000000", 
                    Nombres = "ADMINISTRADOR", 
                    ApellidoPaterno = "SISTEMA",
                    ApellidoMaterno = "BVE",
                    CorreoInstitucional = "admin@bve.com",
                    Carrera = "SOPORTE"
                };
                _context.Alumnos.Add(admin);
                await _context.SaveChangesAsync();
            }

            foreach (var equipo in equipos)
            {
                var sesionActiva = await _context.Sesiones
                    .Include(s => s.Alumno)
                    .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);
                
                if (sesionActiva != null)
                {
                    if (onlyFree)
                    {
                        continue;
                    }

                    sesionActiva.HoraFin = now;
                    if (sesionActiva.Alumno != null)
                    {
                        var sesionesHoy = await _context.Sesiones
                            .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == today && s.HoraFin != null)
                            .ToListAsync();
                        double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                        sesionActiva.Alumno.Estado = segundosConsumidosHoy < (sesionActiva.Alumno.LimiteDiarioSegundos - 60);
                    }
                }

                var sesion = new Sesion
                {
                    AlumnoID = admin.AlumnoID,
                    EquipoID = equipo.EquipoID,
                    Fecha = today,
                    HoraInicio = now,
                    HoraLimite = now.AddHours(5)
                };
                _context.Sesiones.Add(sesion);
                await _context.SaveChangesAsync();

                PendingUnlocks[equipo.NombreRed] = sesion.SesionID;
            }

            return Ok(new { message = "Comando de desbloqueo enviado a todos los equipos." });
        }

        [HttpPost("trigger-remote-shutdown/{nombreRed}")]
        public async Task<IActionResult> TriggerRemoteShutdown(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            var sesionActiva = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);
            if (sesionActiva != null)
            {
                sesionActiva.HoraFin = TimeHelper.GetPeruTime();
                await _context.SaveChangesAsync();

                if (sesionActiva.Alumno != null)
                {
                    var sesionesHoy = await _context.Sesiones
                        .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                        .ToListAsync();

                    double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                    
                    if (segundosConsumidosHoy >= (sesionActiva.Alumno.LimiteDiarioSegundos - 60))
                    {
                        sesionActiva.Alumno.Estado = false;
                    }
                    else
                    {
                        sesionActiva.Alumno.Estado = true;
                    }
                }
            }

            PendingShutdowns[nombreRed] = TimeHelper.GetPeruTime();
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Comando de apagado enviado al equipo {nombreRed}." });
        }

        [HttpPost("trigger-remote-shutdown-all")]
        public async Task<IActionResult> TriggerRemoteShutdownAll()
        {
            var equipos = await _context.Equipos.ToListAsync();
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

            foreach (var e in equipos)
            {
                PendingShutdowns[e.NombreRed] = now;

                var sesionActiva = await _context.Sesiones
                    .Include(s => s.Alumno)
                    .FirstOrDefaultAsync(s => s.EquipoID == e.EquipoID && s.HoraFin == null);

                if (sesionActiva != null)
                {
                    sesionActiva.HoraFin = now;

                    if (sesionActiva.Alumno != null)
                    {
                        var sesionesHoy = await _context.Sesiones
                            .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == today && s.HoraFin != null)
                            .ToListAsync();

                        double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);

                        sesionActiva.Alumno.Estado = segundosConsumidosHoy < (sesionActiva.Alumno.LimiteDiarioSegundos - 60);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando de apagado enviado a todos los equipos." });
        }

        [HttpGet("check-remote-unlock/{nombreRed}")]
        public IActionResult CheckRemoteUnlock(string nombreRed)
        {
            bool shouldShutdown = false;
            if (PendingShutdowns.TryGetValue(nombreRed, out DateTime shutdownTime))
            {
                if ((TimeHelper.GetPeruTime() - shutdownTime).TotalMinutes <= 10)
                {
                    shouldShutdown = true;
                }
                PendingShutdowns.TryRemove(nombreRed, out _);
            }

            if (PendingUnlocks.TryGetValue(nombreRed, out int sesionId))
            {
                PendingUnlocks.TryRemove(nombreRed, out _);
                return Ok(new { unlock = true, sesionId = sesionId, shutdown = shouldShutdown });
            }
            return Ok(new { unlock = false, shutdown = shouldShutdown });
        }

        [HttpGet("check-remote-shutdown/{nombreRed}")]
        public IActionResult CheckRemoteShutdown(string nombreRed)
        {
            bool shouldShutdown = false;
            if (PendingShutdowns.TryGetValue(nombreRed, out DateTime shutdownTime))
            {
                if ((TimeHelper.GetPeruTime() - shutdownTime).TotalMinutes <= 10)
                {
                    shouldShutdown = true;
                }
                PendingShutdowns.TryRemove(nombreRed, out _);
            }
            return Ok(new { shutdown = shouldShutdown });
        }
    }
}
