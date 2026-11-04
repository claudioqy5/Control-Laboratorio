namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Helpers;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

[Collection("SequentialAuthTests")]
public class AuthControllerLoginTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerLoginTests()
    {
        TestDbContextFactory.ResetActiveSessionPings();
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AdminSettings:Username", "admin" },
                { "AdminSettings:Password", "admin123" }
            })
            .Build();
        _controller = new AuthController(_context, _configuration);
    }

    public void Dispose()
    {
        TestDbContextFactory.ResetActiveSessionPings();
        _context.Dispose();
    }

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithSessionDetails()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", nombres: "Juan", apellidoPaterno: "Perez", apellidoMaterno: "Gomez"));
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        var sesionId = data.GetProperty("sesionId").GetInt32();
        sesionId.Should().BeGreaterThan(0);
        data.GetProperty("remainingSeconds").GetDouble().Should().Be(10800);
        data.GetProperty("alumno").GetProperty("nombres").GetString().Should().Be("Juan");
        data.GetProperty("alumno").GetProperty("apellidos").GetString().Should().Be("Perez Gomez");

        _context.Sesiones.Count(s => s.SesionID == sesionId && s.HoraFin == null).Should().Be(1);
        AuthController.ActiveSessionPings.Should().ContainKey(sesionId);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "99999999", DNI = "00000000", NombreRed = "PC-01" });

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Credenciales incorrectas.");
    }

    [Fact]
    public async Task Login_WithInactiveStudent_Returns401Unauthorized()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", estado: false));
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("El alumno se encuentra inactivo.");
        _context.Sesiones.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_WithInactiveEquipment_Returns400BadRequest()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01", estado: false));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var data = ToJsonElement(badRequestResult.Value);
        data.GetProperty("message").GetString().Should().Be("Este equipo está fuera de servicio.");
        _context.Sesiones.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_OnSameEquipmentWithin3Minutes_ReusesActiveSession()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var activeSesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaInicio: TimeHelper.GetPeruTime().AddMinutes(-1),
            horaLimite: TimeHelper.GetPeruTime().AddHours(2).AddMinutes(59)));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("sesionId").GetInt32().Should().Be(activeSesion.SesionID);
        _context.Sesiones.Count().Should().Be(1);
        AuthController.ActiveSessionPings.Should().ContainKey(activeSesion.SesionID);
    }

    [Fact]
    public async Task Login_OnEquipmentWithOrphanSession_ClosesOrphanSession()
    {
        var alumno1 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var alumno2 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "87654321"));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var orphanSesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno1.AlumnoID,
            equipoId: equipo.EquipoID,
            horaInicio: TimeHelper.GetPeruTime().AddMinutes(-10)));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240002", DNI = "87654321", NombreRed = "PC-01" });

        result.Should().BeOfType<OkObjectResult>();
        var reloadedOrphan = await _context.Sesiones.FindAsync(orphanSesion.SesionID);
        reloadedOrphan!.HoraFin.Should().NotBeNull();

        _context.Sesiones.Count(s => s.AlumnoID == alumno2.AlumnoID && s.HoraFin == null).Should().Be(1);
    }

    [Fact]
    public async Task Login_WhenStudentHasActiveSessionOnAnotherEquipment_ClosesRemoteSession()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var equipo1 = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var equipo2 = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-02"));

        var remoteSesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo1.EquipoID,
            horaInicio: TimeHelper.GetPeruTime().AddMinutes(-30)));
        AuthController.ActiveSessionPings[remoteSesion.SesionID] = TimeHelper.GetPeruTime();

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-02" });

        result.Should().BeOfType<OkObjectResult>();
        var reloadedRemote = await _context.Sesiones.FindAsync(remoteSesion.SesionID);
        reloadedRemote!.HoraFin.Should().NotBeNull();
        AuthController.ActiveSessionPings.Should().NotContainKey(remoteSesion.SesionID);

        _context.Sesiones.Count(s => s.AlumnoID == alumno.AlumnoID && s.EquipoID == equipo2.EquipoID && s.HoraFin == null).Should().Be(1);
    }

    [Fact]
    public async Task Login_WhenStudentExhaustedQuotaAndIsReactivated_ExtendsQuotaAndReturns200()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", estado: true, limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var pastStart = TimeHelper.GetPeruTime().AddHours(-4);
        var pastEnd = pastStart.AddSeconds(10800);
        TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            fecha: TimeHelper.GetPeruTime().Date,
            horaInicio: pastStart,
            horaFin: pastEnd));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("remainingSeconds").GetDouble().Should().Be(10800);

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.LimiteDiarioSegundos.Should().Be(21600);
    }

    [Fact]
    public async Task Login_WhenRemainingTimeLessThan15MinutesWithPreviousSessions_AddsAutoBonus3Hours()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", estado: true, limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var pastStart = TimeHelper.GetPeruTime().AddHours(-3);
        var pastEnd = pastStart.AddSeconds(10200);
        TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            fecha: TimeHelper.GetPeruTime().Date,
            horaInicio: pastStart,
            horaFin: pastEnd));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-01" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("remainingSeconds").GetDouble().Should().Be(11400);

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.LimiteDiarioSegundos.Should().Be(21600);
    }

    [Fact]
    public async Task Login_WhenEquipmentDoesNotExist_AutoRegistersEquipmentAndSucceeds()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));

        var result = await _controller.Login(new LoginRequest { CodigoUniversitario = "20240001", DNI = "12345678", NombreRed = "PC-NEW-AUTO" });

        result.Should().BeOfType<OkObjectResult>();
        var registeredEquipo = _context.Equipos.FirstOrDefault(e => e.NombreRed == "PC-NEW-AUTO");
        registeredEquipo.Should().NotBeNull();
        registeredEquipo!.Estado.Should().BeTrue();
        registeredEquipo.Ubicacion.Should().Be("Laboratorio Central");
    }
}
