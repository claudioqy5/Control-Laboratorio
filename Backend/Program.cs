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

            IF OBJECT_ID('t_conferencia_medica', 'U') IS NULL
            BEGIN
                CREATE TABLE t_conferencia_medica (
                    id_conferencia INT IDENTITY(1,1) PRIMARY KEY,
                    titulo_evento NVARCHAR(250) NOT NULL,
                    expositor_ponente NVARCHAR(200) NOT NULL,
                    entidad_editorial NVARCHAR(150) NULL,
                    fecha_hora_inicio DATETIME2 NOT NULL,
                    fecha_hora_fin DATETIME2 NOT NULL,
                    modalidad NVARCHAR(20) NOT NULL,
                    ubicacion_o_enlace NVARCHAR(500) NOT NULL,
                    asistencia_abierta BIT NOT NULL,
                    estado_evento NVARCHAR(20) NOT NULL,
                    auto_purgar_30_dias BIT NOT NULL,
                    fecha_caducidad_purge DATETIME2 NULL,
                    fecha_creacion DATETIME2 NOT NULL
                );
            END

            IF OBJECT_ID('PARTICIPACIONES_CONFERENCIA', 'U') IS NULL
            BEGIN
                CREATE TABLE PARTICIPACIONES_CONFERENCIA (
                    IdParticipacion INT IDENTITY(1,1) PRIMARY KEY,
                    IdConferencia INT NOT NULL,
                    TipoParticipante NVARCHAR(20) NOT NULL,
                    TipoDocumento NVARCHAR(20) NOT NULL,
                    NumeroDocumento NVARCHAR(20) NOT NULL,
                    CodigoURP NVARCHAR(20) NULL,
                    Nombres NVARCHAR(100) NOT NULL,
                    ApellidoPaterno NVARCHAR(100) NOT NULL,
                    ApellidoMaterno NVARCHAR(100) NULL,
                    Correo NVARCHAR(100) NOT NULL,
                    FechaHoraInscripcion DATETIME2 NOT NULL,
                    FechaHoraAsistencia DATETIME2 NULL,
                    EstadoParticipacion TINYINT NOT NULL,
                    AlumnoID INT NULL,
                    CONSTRAINT FK_PARTICIPACIONES_CONFERENCIA_t_conferencia_medica_IdConferencia FOREIGN KEY (IdConferencia) REFERENCES t_conferencia_medica(id_conferencia) ON DELETE CASCADE,
                    CONSTRAINT FK_PARTICIPACIONES_CONFERENCIA_Alumnos_AlumnoID FOREIGN KEY (AlumnoID) REFERENCES Alumnos(AlumnoID) ON DELETE SET NULL
                );
            END

            IF OBJECT_ID('PARTICIPACIONES_CONFERENCIA', 'U') IS NOT NULL
            BEGIN
                IF OBJECT_ID('FK_PARTICIPACIONES_CONFERENCIA_t_conferencia_medica_IdConferencia', 'F') IS NULL
                BEGIN
                    ALTER TABLE PARTICIPACIONES_CONFERENCIA ADD CONSTRAINT FK_PARTICIPACIONES_CONFERENCIA_t_conferencia_medica_IdConferencia FOREIGN KEY (IdConferencia) REFERENCES t_conferencia_medica(id_conferencia) ON DELETE CASCADE;
                END

                IF OBJECT_ID('FK_PARTICIPACIONES_CONFERENCIA_Alumnos_AlumnoID', 'F') IS NULL
                BEGIN
                    ALTER TABLE PARTICIPACIONES_CONFERENCIA ADD CONSTRAINT FK_PARTICIPACIONES_CONFERENCIA_Alumnos_AlumnoID FOREIGN KEY (AlumnoID) REFERENCES Alumnos(AlumnoID) ON DELETE SET NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PARTICIPACIONES_CONFERENCIA_AlumnoID' AND object_id = OBJECT_ID('PARTICIPACIONES_CONFERENCIA'))
                BEGIN
                    CREATE INDEX IX_PARTICIPACIONES_CONFERENCIA_AlumnoID ON PARTICIPACIONES_CONFERENCIA(AlumnoID);
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento' AND object_id = OBJECT_ID('PARTICIPACIONES_CONFERENCIA'))
                BEGIN
                    CREATE UNIQUE INDEX IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento ON PARTICIPACIONES_CONFERENCIA(IdConferencia, NumeroDocumento);
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento_CodigoURP' AND object_id = OBJECT_ID('PARTICIPACIONES_CONFERENCIA'))
                BEGIN
                    CREATE INDEX IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento_CodigoURP ON PARTICIPACIONES_CONFERENCIA(IdConferencia, NumeroDocumento, CodigoURP);
                END
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

            IF OBJECT_ID('BOLETIN_SLIDES', 'U') IS NULL
            BEGIN
                CREATE TABLE BOLETIN_SLIDES (
                    IdSlide INT IDENTITY(1,1) PRIMARY KEY,
                    Titulo NVARCHAR(200) NOT NULL,
                    Descripcion NVARCHAR(1000) NULL,
                    ImagenUrl NVARCHAR(500) NOT NULL,
                    Formato TINYINT NOT NULL,
                    EnlaceUrl NVARCHAR(500) NULL,
                    CONSTRAINT CK_BOLETIN_SLIDES_Formato CHECK (Formato IN (1, 2)),
                    CONSTRAINT CK_BOLETIN_SLIDES_Descripcion CHECK (
                        (Formato = 2 AND Descripcion IS NOT NULL AND LTRIM(RTRIM(Descripcion)) <> '')
                        OR (Formato = 1)
                    )
                );
            END

            IF OBJECT_ID('t_base_datos_medica', 'U') IS NULL
            BEGIN
                CREATE TABLE t_base_datos_medica (
                    id_base_datos INT IDENTITY(1,1) PRIMARY KEY,
                    nombre_recurso NVARCHAR(150) NOT NULL,
                    logotipo_url NVARCHAR(500) NOT NULL,
                    descripcion_clinica NVARCHAR(1000) NOT NULL,
                    es_suscripcion BIT NOT NULL DEFAULT 1,
                    tiene_app_movil BIT NOT NULL DEFAULT 0,
                    url_externo NVARCHAR(500) NULL,
                    estado_activo BIT NOT NULL DEFAULT 1,
                    mostrar_en_hexagonos BIT NOT NULL DEFAULT 0,
                    youtube_video_id NVARCHAR(50) NULL
                );
            END

            IF OBJECT_ID('t_base_datos_medica', 'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('t_base_datos_medica', 'youtube_video_id') IS NULL
                BEGIN
                    ALTER TABLE t_base_datos_medica ADD youtube_video_id NVARCHAR(50) NULL;
                END
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_base_datos_medica_estado_activo' AND object_id = OBJECT_ID('t_base_datos_medica'))
            BEGIN
                CREATE INDEX IX_t_base_datos_medica_estado_activo ON t_base_datos_medica(estado_activo);
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_base_datos_medica_mostrar_en_hexagonos' AND object_id = OBJECT_ID('t_base_datos_medica'))
            BEGIN
                CREATE INDEX IX_t_base_datos_medica_mostrar_en_hexagonos ON t_base_datos_medica(mostrar_en_hexagonos);
            END

            IF OBJECT_ID('t_curso_medicina', 'U') IS NULL
            BEGIN
                CREATE TABLE t_curso_medicina (
                    id_curso INT IDENTITY(1,1) PRIMARY KEY,
                    nombre_curso NVARCHAR(150) NOT NULL,
                    ciclo INT NOT NULL,
                    codigo_curso NVARCHAR(20) NULL
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_curso_medicina_ciclo' AND object_id = OBJECT_ID('t_curso_medicina'))
            BEGIN
                CREATE INDEX IX_t_curso_medicina_ciclo ON t_curso_medicina(ciclo);
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_curso_medicina_nombre_curso' AND object_id = OBJECT_ID('t_curso_medicina'))
            BEGIN
                CREATE INDEX IX_t_curso_medicina_nombre_curso ON t_curso_medicina(nombre_curso);
            END

            IF OBJECT_ID('t_recurso_curso', 'U') IS NULL
            BEGIN
                CREATE TABLE t_recurso_curso (
                    id_base_datos INT NOT NULL,
                    id_curso INT NOT NULL,
                    CONSTRAINT PK_t_recurso_curso PRIMARY KEY (id_base_datos, id_curso),
                    CONSTRAINT FK_t_recurso_curso_base_datos FOREIGN KEY (id_base_datos) REFERENCES t_base_datos_medica(id_base_datos) ON DELETE CASCADE,
                    CONSTRAINT FK_t_recurso_curso_curso FOREIGN KEY (id_curso) REFERENCES t_curso_medicina(id_curso) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_recurso_curso_id_curso' AND object_id = OBJECT_ID('t_recurso_curso'))
            BEGIN
                CREATE INDEX IX_t_recurso_curso_id_curso ON t_recurso_curso(id_curso);
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_t_recurso_curso_id_base_datos' AND object_id = OBJECT_ID('t_recurso_curso'))
            BEGIN
                CREATE INDEX IX_t_recurso_curso_id_base_datos ON t_recurso_curso(id_base_datos);
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

        if (!db.CursosMedicina.Any())
        {
            var cursosOficiales = new List<CursoMedicina>
            {
                new CursoMedicina { Ciclo = 1, NombreCurso = "Biología Celular y Molecular", CodigoCurso = "MH-0110" },
                new CursoMedicina { Ciclo = 1, NombreCurso = "Química General", CodigoCurso = "MH-0103" },
                new CursoMedicina { Ciclo = 1, NombreCurso = "Biofísica Médica", CodigoCurso = "MH-0104" },
                new CursoMedicina { Ciclo = 1, NombreCurso = "Matemática Aplicada a la Medicina", CodigoCurso = "MH-0102" },
                new CursoMedicina { Ciclo = 1, NombreCurso = "Comunicación y Redacción Científica", CodigoCurso = "MH-0101" },
                new CursoMedicina { Ciclo = 1, NombreCurso = "Métodos de Estudio Universitario", CodigoCurso = "MH-0105" },

                new CursoMedicina { Ciclo = 2, NombreCurso = "Bioestadística y Demografía Médica", CodigoCurso = "MH-0201" },
                new CursoMedicina { Ciclo = 2, NombreCurso = "Telemática Médica e Informática Biomédica", CodigoCurso = "MH-0202" },
                new CursoMedicina { Ciclo = 2, NombreCurso = "Historia de la Medicina", CodigoCurso = "MH-0203" },
                new CursoMedicina { Ciclo = 2, NombreCurso = "Psicología Médica", CodigoCurso = "MH-0204" },
                new CursoMedicina { Ciclo = 2, NombreCurso = "Bioética y Deontología Médica", CodigoCurso = "MH-0205" },
                new CursoMedicina { Ciclo = 2, NombreCurso = "Metodología de la Investigación Científica", CodigoCurso = "MH-0206" },

                new CursoMedicina { Ciclo = 3, NombreCurso = "Anatomía Humana I", CodigoCurso = "MH-0301" },
                new CursoMedicina { Ciclo = 3, NombreCurso = "Histología Médica", CodigoCurso = "MH-0302" },
                new CursoMedicina { Ciclo = 3, NombreCurso = "Embriología y Genética Humana", CodigoCurso = "MH-0303" },
                new CursoMedicina { Ciclo = 3, NombreCurso = "Inglés Médico I", CodigoCurso = "MH-0304" },

                new CursoMedicina { Ciclo = 4, NombreCurso = "Anatomía Humana II y Neuroanatomía", CodigoCurso = "MH-0401" },
                new CursoMedicina { Ciclo = 4, NombreCurso = "Fisiología Humana", CodigoCurso = "MH-0402" },
                new CursoMedicina { Ciclo = 4, NombreCurso = "Bioquímica y Nutrición Médica", CodigoCurso = "MH-0403" },
                new CursoMedicina { Ciclo = 4, NombreCurso = "Microbiología y Parasitología Médica", CodigoCurso = "MH-0404" },
                new CursoMedicina { Ciclo = 4, NombreCurso = "Inglés Médico II", CodigoCurso = "MH-0405" },

                new CursoMedicina { Ciclo = 5, NombreCurso = "Farmacología Médica", CodigoCurso = "MH-0501" },
                new CursoMedicina { Ciclo = 5, NombreCurso = "Patología General e Inmunología", CodigoCurso = "MH-0502" },
                new CursoMedicina { Ciclo = 5, NombreCurso = "Fisiopatología", CodigoCurso = "MH-0503" },
                new CursoMedicina { Ciclo = 5, NombreCurso = "Epidemiología y Salud Pública", CodigoCurso = "MH-0504" },

                new CursoMedicina { Ciclo = 6, NombreCurso = "Semiología Médica y Propedéutica Clínica", CodigoCurso = "MH-0601" },
                new CursoMedicina { Ciclo = 6, NombreCurso = "Patología Especial", CodigoCurso = "MH-0602" },
                new CursoMedicina { Ciclo = 6, NombreCurso = "Diagnóstico por Imágenes y Radiología", CodigoCurso = "MH-0603" },
                new CursoMedicina { Ciclo = 6, NombreCurso = "Patología Clínica y Medicina de Laboratorio", CodigoCurso = "MH-0604" },

                new CursoMedicina { Ciclo = 7, NombreCurso = "Medicina Interna I", CodigoCurso = "MH-0701" },
                new CursoMedicina { Ciclo = 7, NombreCurso = "Infectología y Enfermedades Tropicales", CodigoCurso = "MH-0702" },
                new CursoMedicina { Ciclo = 7, NombreCurso = "Dermatología Clínica", CodigoCurso = "MH-0703" },
                new CursoMedicina { Ciclo = 7, NombreCurso = "Terapéutica Médica", CodigoCurso = "MH-0704" },

                new CursoMedicina { Ciclo = 8, NombreCurso = "Medicina Interna II", CodigoCurso = "MH-0801" },
                new CursoMedicina { Ciclo = 8, NombreCurso = "Neurología Clínica", CodigoCurso = "MH-0802" },
                new CursoMedicina { Ciclo = 8, NombreCurso = "Toxicología Clínica", CodigoCurso = "MH-0803" },

                new CursoMedicina { Ciclo = 9, NombreCurso = "Cirugía General y del Aparato Digestivo", CodigoCurso = "MH-0901" },
                new CursoMedicina { Ciclo = 9, NombreCurso = "Anestesiología y Reanimación", CodigoCurso = "MH-0902" },
                new CursoMedicina { Ciclo = 9, NombreCurso = "Traumatología y Ortopedia", CodigoCurso = "MH-0903" },
                new CursoMedicina { Ciclo = 9, NombreCurso = "Urología", CodigoCurso = "MH-0904" },

                new CursoMedicina { Ciclo = 10, NombreCurso = "Cirugía de Especialidades", CodigoCurso = "MH-1001" },
                new CursoMedicina { Ciclo = 10, NombreCurso = "Oftalmología", CodigoCurso = "MH-1002" },
                new CursoMedicina { Ciclo = 10, NombreCurso = "Otorrinolaringología", CodigoCurso = "MH-1003" },
                new CursoMedicina { Ciclo = 10, NombreCurso = "Medicina Física y Rehabilitación", CodigoCurso = "MH-1004" },
                new CursoMedicina { Ciclo = 10, NombreCurso = "Psiquiatría y Salud Mental", CodigoCurso = "MH-1005" },

                new CursoMedicina { Ciclo = 11, NombreCurso = "Ginecología y Obstetricia", CodigoCurso = "MH-1101" },
                new CursoMedicina { Ciclo = 11, NombreCurso = "Medicina Legal y Forense", CodigoCurso = "MH-1102" },
                new CursoMedicina { Ciclo = 11, NombreCurso = "Gerencia y Gestión en Servicios de Salud", CodigoCurso = "MH-1103" },

                new CursoMedicina { Ciclo = 12, NombreCurso = "Pediatría y Neonatología", CodigoCurso = "MH-1201" },
                new CursoMedicina { Ciclo = 12, NombreCurso = "Salud Comunitaria y Atención Primaria", CodigoCurso = "MH-1202" },
                new CursoMedicina { Ciclo = 12, NombreCurso = "Taller de Tesis y Proyectos de Investigación", CodigoCurso = "MH-1203" },

                new CursoMedicina { Ciclo = 13, NombreCurso = "Internado Médico I - Medicina Interna", CodigoCurso = "MH-1301" },
                new CursoMedicina { Ciclo = 13, NombreCurso = "Internado Médico I - Pediatría", CodigoCurso = "MH-1302" },

                new CursoMedicina { Ciclo = 14, NombreCurso = "Internado Médico II - Cirugía General", CodigoCurso = "MH-1401" },
                new CursoMedicina { Ciclo = 14, NombreCurso = "Internado Médico II - Ginecología y Obstetricia", CodigoCurso = "MH-1402" }
            };

            db.CursosMedicina.AddRange(cursosOficiales);
            db.SaveChanges();
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
var boletinesStaticPath = Path.Combine(wwwrootPath, "boletines");
if (!Directory.Exists(boletinesStaticPath))
{
    Directory.CreateDirectory(boletinesStaticPath);
}
var recursosStaticPath = Path.Combine(wwwrootPath, "recursos");
if (!Directory.Exists(recursosStaticPath))
{
    Directory.CreateDirectory(recursosStaticPath);
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
