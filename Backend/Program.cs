using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar el BackgroundService para la reactivación automática
builder.Services.AddHostedService<ReactivacionAlumnosService>();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ControlLaboratorioConnection")));

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Auto-add column if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Crear base de datos y tablas si no existen
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
            
            IF COL_LENGTH('Equipos', 'PosicionMapa') IS NULL
            BEGIN
                ALTER TABLE Equipos ADD PosicionMapa INT NULL
            END
        ");

        // Force cleanup and re-seeding if we want to ensure fresh mock data with all new columns populated (only in development)
        if (app.Environment.IsDevelopment() && db.Sesiones.Count() < 15)
        {
            db.Database.ExecuteSqlRaw("DELETE FROM Sesiones; DELETE FROM Alumnos; DELETE FROM Equipos;");
        }

        // Seeding mock data if empty (only in development)
        if (app.Environment.IsDevelopment())
        {
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
                var hoy = DateTime.Now.Date;
                
                var random = new Random();
                var sesionesMock = new List<Sesion>();
                
                // Generar datos para los últimos 7 días
                for (int diaOffset = -6; diaOffset <= 0; diaOffset++)
                {
                    var fechaDia = hoy.AddDays(diaOffset);
                    
                    // Menos afluencia los fines de semana
                    if (fechaDia.DayOfWeek == DayOfWeek.Saturday || fechaDia.DayOfWeek == DayOfWeek.Sunday)
                    {
                        if (random.Next(0, 3) > 0) continue; // Omitir la mayoría de sábados y domingos
                    }

                    // Cantidad de sesiones para este día (entre 3 y 8)
                    int cantidadSesiones = random.Next(3, 8);
                    var horasDisponibles = new[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

                    for (int j = 0; j < cantidadSesiones; j++)
                    {
                        var hora = horasDisponibles[random.Next(horasDisponibles.Length)];
                        var horaInicio = fechaDia.AddHours(hora).AddMinutes(random.Next(0, 59));
                        var duracion = random.Next(30, 180);
                        var horaFin = horaInicio.AddMinutes(duracion);
                        
                        // Solo dejar algunas sesiones activas si es el día de hoy
                        bool esActiva = (diaOffset == 0 && (j == 1 || j == 3));
                        
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
    }
    catch { /* Ignore if it fails or already exists */ }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
