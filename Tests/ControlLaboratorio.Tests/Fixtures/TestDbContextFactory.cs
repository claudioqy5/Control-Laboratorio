namespace ControlLaboratorio.Tests.Fixtures;

using System;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using ControlLaboratorio.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var dbName = string.IsNullOrWhiteSpace(databaseName) ? Guid.NewGuid().ToString() : databaseName;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static ApplicationDbContext Create(string? dbName = null) => CreateInMemoryDbContext(dbName);

    public static void ResetDatabase(ApplicationDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.ChangeTracker.Clear();
    }

    public static void ResetActiveSessionPings() => AuthController.ActiveSessionPings.Clear();

    public static Alumno CreateAlumno(
        int id = 0, string codigo = "20240001", string dni = "12345678",
        string nombres = "Juan", string apellidoPaterno = "Perez", string apellidoMaterno = "Gomez",
        string carrera = "Medicina Humana", string? telefono = "999888777",
        string? correoInstitucional = "juan.perez@test.edu.pe", string? correoPersonal = "juan.personal@test.com",
        bool estado = true, int limiteSegundos = 10800) => new Alumno
    {
        AlumnoID = id, CodigoUniversitario = codigo, DNI = dni,
        Nombres = nombres, ApellidoPaterno = apellidoPaterno, ApellidoMaterno = apellidoMaterno,
        Carrera = carrera, Telefono = telefono,
        CorreoInstitucional = correoInstitucional, CorreoPersonal = correoPersonal,
        Estado = estado, LimiteDiarioSegundos = limiteSegundos
    };

    public static Equipo CreateEquipo(
        int id = 0, string nombreRed = "PC-01", string ubicacion = "Laboratorio Central",
        bool estado = true, int? posicionMapa = 1, string? @alias = "PC-01",
        string? comentario = "Equipo de pruebas", string? agentVersion = "1.0.0",
        DateTime? agentVersionFecha = null) => new Equipo
    {
        EquipoID = id, NombreRed = nombreRed, Ubicacion = ubicacion,
        Estado = estado, PosicionMapa = posicionMapa, Alias = @alias,
        Comentario = comentario, AgentVersion = agentVersion,
        AgentVersionFecha = agentVersionFecha ?? TimeHelper.GetPeruTime()
    };

    public static Sesion CreateSesion(
        int id = 0, int alumnoId = 1, int equipoId = 1,
        DateTime? fecha = null, DateTime? horaInicio = null,
        DateTime? horaFin = null, DateTime? horaLimite = null)
    {
        var baseDate = TimeHelper.GetPeruTime();
        return new Sesion
        {
            SesionID = id, AlumnoID = alumnoId, EquipoID = equipoId,
            Fecha = fecha ?? baseDate.Date,
            HoraInicio = horaInicio ?? baseDate,
            HoraFin = horaFin,
            HoraLimite = horaLimite ?? baseDate.AddHours(3)
        };
    }

    public static Alumno SeedAlumno(ApplicationDbContext context, Alumno? alumno = null)
    {
        var entity = alumno ?? CreateAlumno();
        context.Alumnos.Add(entity);
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return entity;
    }

    public static Equipo SeedEquipo(ApplicationDbContext context, Equipo? equipo = null)
    {
        var entity = equipo ?? CreateEquipo();
        context.Equipos.Add(entity);
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return entity;
    }

    public static Sesion SeedSesion(ApplicationDbContext context, Sesion? sesion = null)
    {
        var entity = sesion ?? CreateSesion();
        context.Sesiones.Add(entity);
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return entity;
    }

    public static (Alumno Alumno, Equipo Equipo, Sesion Sesion) SeedActiveEnvironment(
        ApplicationDbContext context, string codigo = "20240001", string dni = "12345678", string pcNombre = "PC-01")
    {
        var alumno = SeedAlumno(context, CreateAlumno(codigo: codigo, dni: dni));
        var equipo = SeedEquipo(context, CreateEquipo(nombreRed: pcNombre));
        var sesion = SeedSesion(context, CreateSesion(alumnoId: alumno.AlumnoID, equipoId: equipo.EquipoID));
        return (alumno, equipo, sesion);
    }
}
