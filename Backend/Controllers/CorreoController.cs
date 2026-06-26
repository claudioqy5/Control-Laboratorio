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

            // Verificar si el logo existe antes de iniciar el ciclo de envío (admite .jpg y .png)
            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logourp.jpg");
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logourp.png");
            }
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logourp.jpg");
            }
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logourp.png");
            }
            bool hasLogo = System.IO.File.Exists(logoPath);
            string contentType = logoPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

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
                        // Personalizar Asunto y Mensaje por cada alumno
                        string nombreCompleto = $"{alumno.Nombres} {alumno.ApellidoPaterno} {alumno.ApellidoMaterno}".Trim();
                        string personalizedSubject = request.Asunto
                            .Replace("{Nombre}", alumno.Nombres)
                            .Replace("{NombreCompleto}", nombreCompleto);

                        string personalizedBody = request.Mensaje
                            .Replace("{Nombre}", alumno.Nombres)
                            .Replace("{NombreCompleto}", nombreCompleto);

                        // Si el mensaje viene en texto plano, cambiar saltos de línea por <br/>
                        if (!request.Mensaje.Contains("<p>") && !request.Mensaje.Contains("<br/>") && !request.Mensaje.Contains("</div>"))
                        {
                            personalizedBody = personalizedBody.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                        }

                        string logoHtml = hasLogo ? "<br/><img src=\"cid:logourp\" alt=\"Logo URP\" style=\"max-width: 260px; height: auto; margin-top: 12px;\" />" : "";

                        // Estructura de firma institucional
                        string signatureHtml = $@"
<br/><br/>
<hr style=""border: none; border-top: 1px solid #e0e0e0; margin-top: 20px;"" />
<div style=""font-family: Arial, sans-serif; font-size: 13px; color: #444; line-height: 1.6;"">
    <strong style=""color: #0b3c5d; font-size: 14px;"">Biblioteca Virtual y Especializada FAMURP</strong><br/>
    <strong>Teléfono Central:</strong> (01) 708-0000, <strong>Anexo de la Biblioteca:</strong> 6312<br/>
    <strong>Jefa de Biblioteca:</strong> Lic. Francisca Valero Villaizan<br/>
    <strong>Horario de atención:</strong> Lunes a Viernes de 8:00 a.m. a 9:00 p.m. / Sábados de 8:00 a.m. to 2:00 p.m.
    {logoHtml}
</div>";

                        string fullHtmlBody = personalizedBody + signatureHtml;

                        using (var mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(senderEmail, senderName);
                            mailMessage.Subject = personalizedSubject;
                            mailMessage.To.Add(destinatario);

                            // Agregar adjuntos desde memoria
                            foreach (var adjunto in adjuntos)
                            {
                                var ms = new MemoryStream(adjunto.Bytes);
                                var attachment = new Attachment(ms, adjunto.FileName, adjunto.ContentType);
                                mailMessage.Attachments.Add(attachment);
                            }

                            if (hasLogo)
                            {
                                // Para imágenes en línea (inline images) usamos AlternateView
                                var htmlView = AlternateView.CreateAlternateViewFromString(fullHtmlBody, null, "text/html");
                                var logoResource = new LinkedResource(logoPath, contentType)
                                {
                                    ContentId = "logourp"
                                };
                                htmlView.LinkedResources.Add(logoResource);
                                mailMessage.AlternateViews.Add(htmlView);
                            }
                            else
                            {
                                mailMessage.Body = fullHtmlBody;
                                mailMessage.IsBodyHtml = true;
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
