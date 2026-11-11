using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var secretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("La clave secreta JwtSettings:SecretKey no está configurada.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en formato Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHostedService<ReactivacionAlumnosService>();
builder.Services.AddHostedService<SessionMonitorService>();

builder.Services.AddScoped<GoogleVisionService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ControlLaboratorioConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Sesiones', 'HoraLimite') IS NULL
            BEGIN
                ALTER TABLE Sesiones ADD HoraLimite DATETIME2 NULL
            END
            
            IF COL_LENGTH('Alumnos', 'Telefono') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD Telefono NVARCHAR(20) NULL
            END
            
            IF COL_LENGTH('Alumnos', 'CorreoInstitucional') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD CorreoInstitucional NVARCHAR(100) NULL
            END
            
            IF COL_LENGTH('Alumnos', 'CorreoPersonal') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD CorreoPersonal NVARCHAR(100) NULL
            END

            UPDATE Alumnos 
            SET CorreoInstitucional = CONCAT(RTRIM(LTRIM(CodigoUniversitario)), '@urp.edu.pe')
            WHERE (CorreoInstitucional IS NULL OR LTRIM(RTRIM(CorreoInstitucional)) = '' OR CorreoInstitucional = '-')
              AND CodigoUniversitario IS NOT NULL 
              AND LTRIM(RTRIM(CodigoUniversitario)) <> '';
            
            IF COL_LENGTH('Equipos', 'PosicionMapa') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD PosicionMapa INT NULL
            END

            IF COL_LENGTH('Alumnos', 'LimiteDiarioSegundos') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD LimiteDiarioSegundos INT NOT NULL DEFAULT 10800
            END

            IF COL_LENGTH('Equipos', 'Alias') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD Alias NVARCHAR(50) NULL
            END

            IF COL_LENGTH('Equipos', 'Comentario') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD Comentario NVARCHAR(255) NULL
            END

            IF COL_LENGTH('Equipos', 'AgentVersion') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD AgentVersion NVARCHAR(20) NULL
            END

            IF COL_LENGTH('Equipos', 'AgentVersionFecha') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD AgentVersionFecha DATETIME2 NULL
            END

            IF OBJECT_ID('Categorias', 'U') IS NULL
            BEGIN
                CREATE TABLE Categorias (
                    CategoriaID INT IDENTITY(1,1) PRIMARY KEY,
                    Codigo NVARCHAR(50) NOT NULL,
                    Nombre NVARCHAR(150) NOT NULL,
                    Descripcion NVARCHAR(MAX) NULL
                );
            END

            IF OBJECT_ID('UsuariosAdmin', 'U') IS NULL
            BEGIN
                CREATE TABLE UsuariosAdmin (
                    IdUsuarioAdmin INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(50) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(100) NOT NULL,
                    Nombres NVARCHAR(150) NOT NULL,
                    Correo NVARCHAR(150) NULL,
                    Rol TINYINT NOT NULL DEFAULT 1,
                    EstadoActivo BIT NOT NULL DEFAULT 1,
                    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
                    UltimoAcceso DATETIME2 NULL
                );
            END

            IF OBJECT_ID('RefreshTokens', 'U') IS NULL
            BEGIN
                CREATE TABLE RefreshTokens (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Token NVARCHAR(150) NOT NULL UNIQUE,
                    IdUsuarioAdmin INT NOT NULL,
                    ExpiresAt DATETIME2 NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    RevokedAt DATETIME2 NULL,
                    ReplacedByToken NVARCHAR(150) NULL,
                    CONSTRAINT FK_RefreshTokens_UsuariosAdmin FOREIGN KEY (IdUsuarioAdmin) REFERENCES UsuariosAdmin(IdUsuarioAdmin) ON DELETE CASCADE
                );
            END

            IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Libros_NroRegistro' AND object_id = OBJECT_ID('Libros'))
            BEGIN
                IF INDEXPROPERTY(OBJECT_ID('Libros'), 'IX_Libros_NroRegistro', 'IsUnique') = 1
                BEGIN
                    DROP INDEX IX_Libros_NroRegistro ON Libros;
                    CREATE INDEX IX_Libros_NroRegistro ON Libros(NroRegistro);
                END
            END

            IF OBJECT_ID('OBJETOS_PERDIDOS', 'U') IS NULL
            BEGIN
                CREATE TABLE OBJETOS_PERDIDOS (
                    IdObjeto INT IDENTITY(1,1) PRIMARY KEY,
                    Titulo NVARCHAR(150) NOT NULL,
                    Categoria TINYINT NOT NULL,
                    Descripcion NVARCHAR(MAX) NOT NULL,
                    LugarHallazgo NVARCHAR(150) NOT NULL,
                    FechaHoraHallazgo DATETIME2 NOT NULL,
                    EstadoObjeto TINYINT NOT NULL DEFAULT 1,
                    IdUsuarioHallazgo INT NOT NULL,
                    NombrePersonalHallazgo NVARCHAR(150) NOT NULL,
                    FechaHoraDevolucion DATETIME2 NULL,
                    AlumnoID INT NULL,
                    DocumentoReceptor NVARCHAR(20) NULL,
                    CodigoURPReceptor NVARCHAR(20) NULL,
                    IdUsuarioEntrega INT NULL,
                    NombrePersonalEntrega NVARCHAR(150) NULL,
                    ObservacionesEntrega NVARCHAR(MAX) NULL,
                    FechaBaja DATETIME2 NULL,
                    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
                    CONSTRAINT FK_OBJETOS_PERDIDOS_UsuarioHallazgo FOREIGN KEY (IdUsuarioHallazgo) REFERENCES UsuariosAdmin(IdUsuarioAdmin),
                    CONSTRAINT FK_OBJETOS_PERDIDOS_UsuarioEntrega FOREIGN KEY (IdUsuarioEntrega) REFERENCES UsuariosAdmin(IdUsuarioAdmin),
                    CONSTRAINT FK_OBJETOS_PERDIDOS_Alumno FOREIGN KEY (AlumnoID) REFERENCES Alumnos(AlumnoID) ON DELETE SET NULL,
                    CONSTRAINT CK_OBJETOS_PERDIDOS_ConsistenciaDevolucion CHECK (
                        (EstadoObjeto = 2 AND FechaHoraDevolucion IS NOT NULL AND IdUsuarioEntrega IS NOT NULL AND NombrePersonalEntrega IS NOT NULL AND (AlumnoID IS NOT NULL OR DocumentoReceptor IS NOT NULL))
                        OR
                        (EstadoObjeto <> 2 AND FechaHoraDevolucion IS NULL AND IdUsuarioEntrega IS NULL AND NombrePersonalEntrega IS NULL AND DocumentoReceptor IS NULL AND CodigoURPReceptor IS NULL AND AlumnoID IS NULL)
                    ),
                    CONSTRAINT CK_OBJETOS_PERDIDOS_ConsistenciaBaja CHECK (
                        (EstadoObjeto = 4 AND FechaBaja IS NOT NULL)
                        OR
                        (EstadoObjeto <> 4 AND FechaBaja IS NULL)
                    )
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OBJETOS_PERDIDOS_Estado_FechaHallazgo' AND object_id = OBJECT_ID('OBJETOS_PERDIDOS'))
            BEGIN
                CREATE INDEX IX_OBJETOS_PERDIDOS_Estado_FechaHallazgo ON OBJETOS_PERDIDOS(EstadoObjeto, FechaHoraHallazgo);
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OBJETOS_PERDIDOS_AlumnoID' AND object_id = OBJECT_ID('OBJETOS_PERDIDOS'))
            BEGIN
                CREATE INDEX IX_OBJETOS_PERDIDOS_AlumnoID ON OBJETOS_PERDIDOS(AlumnoID);
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OBJETOS_PERDIDOS_CodigoURPReceptor' AND object_id = OBJECT_ID('OBJETOS_PERDIDOS'))
            BEGIN
                CREATE INDEX IX_OBJETOS_PERDIDOS_CodigoURPReceptor ON OBJETOS_PERDIDOS(CodigoURPReceptor);
            END
        ");

        if (app.Environment.IsDevelopment() && db.Sesiones.Count() < 15)
        {
            db.Database.ExecuteSqlRaw("DELETE FROM Sesiones; DELETE FROM Alumnos; DELETE FROM Equipos;");
        }

        if (app.Environment.IsDevelopment())
        {
            if (!db.Categorias.Any())
            {
                var mockCategorias = new List<Categoria>
                {
                    new Categoria { Codigo = "CAT-001", Nombre = "Medicina General", Descripcion = "Libros de medicina general y fundamentos." },
                    new Categoria { Codigo = "CAT-002", Nombre = "Anatomía", Descripcion = "Estudio de la estructura de los seres vivos." },
                    new Categoria { Codigo = "CAT-003", Nombre = "Fisiología", Descripcion = "Estudio de las funciones y mecanismos de los sistemas vivos." },
                    new Categoria { Codigo = "CAT-004", Nombre = "Patología", Descripcion = "Estudio de las enfermedades." },
                    new Categoria { Codigo = "CAT-005", Nombre = "Dermatología", Descripcion = "Especialidad médica encargada del estudio de la piel." },
                    new Categoria { Codigo = "CAT-006", Nombre = "Pediatría", Descripcion = "Rama de la medicina que involucra la atención médica de bebés, niños y adolescentes." },
                    new Categoria { Codigo = "CAT-007", Nombre = "Cirugía", Descripcion = "Práctica que implica manipulación mecánica de las estructuras anatómicas." }
                };
                db.Categorias.AddRange(mockCategorias);
                db.SaveChanges();
            }

            if (!db.Alumnos.Any())
            {
                var mockAlumnos = new List<Alumno>
                {
                    new Alumno { CodigoUniversitario = "20231001", DNI = "71000001", Nombres = "Juan Alberto", ApellidoPaterno = "Pérez", ApellidoMaterno = "Quispe", Carrera = "Ingeniería de Sistemas", Telefono = "987654321", CorreoInstitucional = "juan.perez@universidad.edu.pe", CorreoPersonal = "juanito@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231002", DNI = "72000002", Nombres = "María Elena", ApellidoPaterno = "Gómez", ApellidoMaterno = "Flores", Carrera = "Ingeniería Industrial", Telefono = "987654322", CorreoInstitucional = "maria.gomez@universidad.edu.pe", CorreoPersonal = "maria@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231003", DNI = "73000003", Nombres = "Carlos Augusto", ApellidoPaterno = "Rodríguez", ApellidoMaterno = "Sánchez", Carrera = "Derecho", Telefono = "987654323", CorreoInstitucional = "carlos.rodriguez@universidad.edu.pe", CorreoPersonal = "carlos@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231004", DNI = "74000004", Nombres = "Ana Sofía", ApellidoPaterno = "Martínez", ApellidoMaterno = "Díaz", Carrera = "Psicología", Telefono = "987654324", CorreoInstitucional = "ana.martinez@universidad.edu.pe", CorreoPersonal = "ana@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231005", DNI = "75000005", Nombres = "Luis Fernando", ApellidoPaterno = "López", ApellidoMaterno = "Torres", Carrera = "Administración", Telefono = "987654325", CorreoInstitucional = "luis.lopez@universidad.edu.pe", CorreoPersonal = "luis@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231006", DNI = "76000006", Nombres = "Gabriela Alejandra", ApellidoPaterno = "Rojas", ApellidoMaterno = "Castro", Carrera = "Ingeniería de Sistemas", Telefono = "987654326", CorreoInstitucional = "gabriela.rojas@universidad.edu.pe", CorreoPersonal = "gaby@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231007", DNI = "77000007", Nombres = "José Manuel", ApellidoPaterno = "Silva", ApellidoMaterno = "Ramírez", Carrera = "Medicina", Telefono = "987654327", CorreoInstitucional = "jose.silva@universidad.edu.pe", CorreoPersonal = "jose@gmail.com", Estado = true },
                    new Alumno { CodigoUniversitario = "20231008", DNI = "78000008", Nombres = "Patricia Isabel", ApellidoPaterno = "Vargas", ApellidoMaterno = "Mendoza", Carrera = "Derecho", Telefono = "987654328", CorreoInstitucional = "patricia.vargas@universidad.edu.pe", CorreoPersonal = "paty@gmail.com", Estado = true }
                };
                db.Alumnos.AddRange(mockAlumnos);
                db.SaveChanges();
            }

            if (!db.Equipos.Any())
            {
                var mockEquipos = Enumerable.Range(1, 10).Select(i => new Equipo
                {
                    NombreRed = $"PC-LAB01-{i:D2}",
                    Ubicacion = "Laboratorio Central - Fila A",
                    Estado = true,
                    PosicionMapa = i
                }).ToList();
                db.Equipos.AddRange(mockEquipos);
                db.SaveChanges();
            }

            if (!db.Sesiones.Any())
            {
                var alumnos = db.Alumnos.ToList();
                var equipos = db.Equipos.ToList();
                var hoy = ControlLaboratorio.API.Helpers.TimeHelper.GetPeruTime().Date;
                
                var random = new Random();
                var sesionesMock = new List<Sesion>();
                
                for (int monthOffset = 0; monthOffset < 12; monthOffset++)
                {
                    var baseDate = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-monthOffset);
                    int daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
                    
                    for (int i = 0; i < 40; i++)
                    {
                        var diaAleatorio = random.Next(1, daysInMonth + 1);
                        var fechaDia = new DateTime(baseDate.Year, baseDate.Month, diaAleatorio);

                        if (fechaDia.DayOfWeek == DayOfWeek.Saturday || fechaDia.DayOfWeek == DayOfWeek.Sunday)
                        {
                            if (random.Next(0, 3) > 0) continue;
                        }

                        var horasDisponibles = new[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
                        var hora = horasDisponibles[random.Next(horasDisponibles.Length)];
                        var horaInicio = fechaDia.AddHours(hora).AddMinutes(random.Next(0, 59));
                        var duracion = random.Next(30, 180);
                        var horaFin = horaInicio.AddMinutes(duracion);
                        
                        bool esActiva = (fechaDia.Date == hoy.Date && i % 5 == 0);
                        
                        sesionesMock.Add(new Sesion
                        {
                            AlumnoID = alumnos[random.Next(alumnos.Count)].AlumnoID,
                            EquipoID = equipos[random.Next(equipos.Count)].EquipoID,
                            Fecha = fechaDia,
                            HoraInicio = horaInicio,
                            HoraFin = esActiva ? null : (DateTime?)horaFin,
                            HoraLimite = horaInicio.AddHours(3)
                        });
                    }
                }
                db.Sesiones.AddRange(sesionesMock);
                db.SaveChanges();
            }
        }


        try
        {
            var portadasDir = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "portadas");
            if (!Directory.Exists(portadasDir))
            {
                Directory.CreateDirectory(portadasDir);
            }

            var librosBase64 = db.Libros.Where(l => l.Portada != null && l.Portada.StartsWith("data:")).ToList();
            if (librosBase64.Any())
            {
                foreach (var libro in librosBase64)
                {
                    try
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(libro.Portada!, @"^data:(image\/[a-zA-Z0-9\+\-]+);base64,(.+)$");
                        if (match.Success)
                        {
                            var mime = match.Groups[1].Value.ToLowerInvariant();
                            var base64Data = match.Groups[2].Value;
                            var ext = mime switch
                            {
                                "image/png" => ".png",
                                "image/webp" => ".webp",
                                _ => ".jpg"
                            };

                            var fileName = $"portada_{libro.LibroID}{ext}";
                            var filePath = Path.Combine(portadasDir, fileName);
                            var bytes = Convert.FromBase64String(base64Data);
                            File.WriteAllBytes(filePath, bytes);

                            libro.Portada = $"/api/static/portadas/{fileName}";
                        }
                    }
                    catch
                    {
                    }
                }
                db.SaveChanges();
            }
        }
        catch
        {
        }
    }
    catch
    {
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api/openapi/{documentName}.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api/openapi/v1.json", "ControlLaboratorio API v1");
        c.RoutePrefix = "api/swagger";
    });

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("BVE FAMURP - API Reference")
               .WithEndpointPrefix("/api/scalar/{documentName}")
               .WithOpenApiRoutePattern("/api/openapi/{documentName}.json");
    });
}

app.UseStaticFiles();

var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var portadasStaticPath = Path.Combine(wwwrootPath, "portadas");
if (!Directory.Exists(portadasStaticPath))
{
    Directory.CreateDirectory(portadasStaticPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    RequestPath = "/api/static",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=2592000");
    }
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
