using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ControlLaboratorio.API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CorreoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CorreoController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("enviar-masivo")]
        public async Task<IActionResult> EnviarCorreoMasivo([FromForm] CorreoMasivoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Asunto) || string.IsNullOrWhiteSpace(request.Mensaje))
            {
                return BadRequest("El asunto y el mensaje son obligatorios.");
            }

            // Obtener todos los alumnos que tengan algún correo electrónico
            var alumnos = await _context.Alumnos
                .Where(a => a.Estado && (!string.IsNullOrEmpty(a.CorreoInstitucional) || !string.IsNullOrEmpty(a.CorreoPersonal)))
                .ToListAsync();

            if (!alumnos.Any())
            {
                return NotFound("No se encontraron alumnos activos con correo registrado.");
            }

            // Obtener configuración SMTP
            var smtpSection = _configuration.GetSection("SmtpSettings");
            var server = smtpSection["Server"];
            var portStr = smtpSection["Port"];
            var senderEmail = smtpSection["SenderEmail"];
            var senderName = smtpSection["SenderName"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var enableSslStr = smtpSection["EnableSsl"];

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(username))
            {
                return StatusCode(500, "La configuración SMTP no está completa en el servidor.");
            }

            int.TryParse(portStr, out int port);
            if (port == 0) port = 587;
            bool.TryParse(enableSslStr, out bool enableSsl);

            // Cargar archivos adjuntos en memoria para evitar problemas de relectura/disposición en el ciclo
            var adjuntos = new List<(byte[] Bytes, string FileName, string ContentType)>();
            if (request.Archivos != null)
            {
                foreach (var archivo in request.Archivos)
                {
                    if (archivo.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await archivo.CopyToAsync(ms);
                            adjuntos.Add((ms.ToArray(), archivo.FileName, archivo.ContentType));
                        }
                    }
                }
            }

            int enviados = 0;
            int fallidos = 0;
            var errores = new List<string>();

            using (var smtpClient = new SmtpClient(server, port))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.EnableSsl = enableSsl;

                foreach (var alumno in alumnos)
                {
                    // Intentar enviar al institucional, sino al personal
                    string destinatario = !string.IsNullOrEmpty(alumno.CorreoInstitucional) 
                        ? alumno.CorreoInstitucional 
                        : alumno.CorreoPersonal!;

                    try
                    {
                        using (var mailMessage = new MailMessage
                        {
                            From = new MailAddress(senderEmail, senderName),
                            Subject = request.Asunto,
                            Body = request.Mensaje,
                            IsBodyHtml = true
                        })
                        {
                            mailMessage.To.Add(destinatario);

                            // Agregar adjuntos desde memoria
                            foreach (var adjunto in adjuntos)
                            {
                                var ms = new MemoryStream(adjunto.Bytes);
                                var attachment = new Attachment(ms, adjunto.FileName, adjunto.ContentType);
                                mailMessage.Attachments.Add(attachment);
                            }

                            await smtpClient.SendMailAsync(mailMessage);
                            enviados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        fallidos++;
                        errores.Add($"Error al enviar a {alumno.Nombres} ({destinatario}): {ex.Message}");
                    }
                }
            }

            if (fallidos > 0 && enviados == 0)
            {
                return StatusCode(500, new { 
                    message = "No se pudo enviar ningún correo. Verifique las credenciales SMTP en appsettings.json.", 
                    detalles = errores 
                });
            }

            return Ok(new { 
                enviados, 
                fallidos, 
                detalles = errores 
            });
        }

        public class CorreoMasivoRequest
        {
            public string Asunto { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
            public List<IFormFile>? Archivos { get; set; }
        }
    }
}
