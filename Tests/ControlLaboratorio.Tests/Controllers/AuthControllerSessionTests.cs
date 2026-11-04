namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Helpers;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

[Collection("SequentialAuthTests")]
public class AuthControllerSessionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerSessionTests()
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
    public async Task Logout_WithNonExistentSession_Returns404NotFound()
    {
        var result = await _controller.Logout(new LogoutRequest { SesionId = 9999 });

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var data = ToJsonElement(notFoundResult.Value);
        data.GetProperty("message").GetString().Should().Be("Sesión no encontrada.");
    }

    [Fact]
    public async Task Logout_WithActiveSession_SetsHoraFinAndClearsPing()
    {
        var (_, _, sesion) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240001", "12345678", "PC-01");
        AuthController.ActiveSessionPings[sesion.SesionID] = TimeHelper.GetPeruTime();

        var result = await _controller.Logout(new LogoutRequest { SesionId = sesion.SesionID });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("message").GetString().Should().Be("Sesión finalizada.");

        var reloadedSesion = await _context.Sesiones.FindAsync(sesion.SesionID);
        reloadedSesion!.HoraFin.Should().NotBeNull();
        AuthController.ActiveSessionPings.Should().NotContainKey(sesion.SesionID);
    }

    [Fact]
    public async Task Logout_WhenQuotaExceeded_SetsStudentEstadoFalse()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", estado: true, limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaInicio: TimeHelper.GetPeruTime().AddHours(-3)));

        await _controller.Logout(new LogoutRequest { SesionId = sesion.SesionID });

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.Estado.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WhenQuotaRemaining_LeavesStudentEstadoTrue()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", estado: true, limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaInicio: TimeHelper.GetPeruTime().AddMinutes(-30)));

        await _controller.Logout(new LogoutRequest { SesionId = sesion.SesionID });

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.Estado.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionStatus_WithNonExistentSession_Returns404NotFound()
    {
        var result = await _controller.GetSessionStatus(9999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSessionStatus_WithActiveSession_UpdatesPingAndCalculatesRemainingSeconds()
    {
        var (_, _, sesion) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240001", "12345678", "PC-01");

        var result = await _controller.GetSessionStatus(sesion.SesionID);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("isFinished").GetBoolean().Should().BeFalse();
        data.GetProperty("remainingSeconds").GetDouble().Should().BeGreaterThan(0);
        AuthController.ActiveSessionPings.Should().ContainKey(sesion.SesionID);
    }

    [Fact]
    public async Task GetSessionStatus_WhenExpired_ClampsRemainingSecondsToZero()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaLimite: TimeHelper.GetPeruTime().AddMinutes(-10)));

        var result = await _controller.GetSessionStatus(sesion.SesionID);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("remainingSeconds").GetDouble().Should().Be(0);
    }

    [Fact]
    public async Task GetActiveSession_WithNonExistentEquipment_Returns404NotFound()
    {
        var result = await _controller.GetActiveSession("PC-NON-EXISTENT");

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var data = ToJsonElement(notFoundResult.Value);
        data.GetProperty("message").GetString().Should().Be("Equipo no encontrado.");
    }

    [Fact]
    public async Task GetActiveSession_WhenNoActiveSession_ReturnsHasActiveSessionFalse()
    {
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var result = await _controller.GetActiveSession("PC-01");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("hasActiveSession").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveSession_WhenOccupied_ReturnsFullDetails()
    {
        var (_, _, sesion) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240001", "12345678", "PC-01");

        var result = await _controller.GetActiveSession("PC-01");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("hasActiveSession").GetBoolean().Should().BeTrue();
        data.GetProperty("sesionId").GetInt32().Should().Be(sesion.SesionID);
        data.GetProperty("remainingSeconds").GetDouble().Should().BeGreaterThan(0);
        data.GetProperty("alumno").GetProperty("nombres").GetString().Should().Be("Juan");
        data.GetProperty("alumno").GetProperty("apellidos").GetString().Should().Be("Perez Gomez");
    }

    [Fact]
    public async Task ExtendSession_WithNonExistentSession_Returns404NotFound()
    {
        var result = await _controller.ExtendSession(new ExtendSessionRequest { SesionId = 9999 });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ExtendSession_WhenSessionFinished_Returns400BadRequest()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaFin: TimeHelper.GetPeruTime()));

        var result = await _controller.ExtendSession(new ExtendSessionRequest { SesionId = sesion.SesionID });

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var data = ToJsonElement(badRequestResult.Value);
        data.GetProperty("message").GetString().Should().Be("La sesión ya ha finalizado.");
    }

    [Fact]
    public async Task ExtendSession_WithActiveSession_AddsQuotaAndExtendsHoraLimite()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var initialLimit = TimeHelper.GetPeruTime().AddHours(1);
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaLimite: initialLimit));

        var result = await _controller.ExtendSession(new ExtendSessionRequest { SesionId = sesion.SesionID });

        result.Should().BeOfType<OkObjectResult>();

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.LimiteDiarioSegundos.Should().Be(21600);

        var reloadedSesion = await _context.Sesiones.FindAsync(sesion.SesionID);
        reloadedSesion!.HoraLimite.Should().BeCloseTo(initialLimit.AddHours(3), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SetSessionLimit_WithNonExistentSession_Returns404NotFound()
    {
        var result = await _controller.SetSessionLimit(new SetSessionLimitRequest { SesionId = 9999, NuevaHoraLimite = DateTime.UtcNow });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetSessionLimit_WhenSessionFinished_Returns400BadRequest()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678"));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID,
            horaFin: TimeHelper.GetPeruTime()));

        var result = await _controller.SetSessionLimit(new SetSessionLimitRequest { SesionId = sesion.SesionID, NuevaHoraLimite = DateTime.UtcNow.AddHours(1) });

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var data = ToJsonElement(badRequestResult.Value);
        data.GetProperty("message").GetString().Should().Be("La sesión ya ha finalizado.");
    }

    [Fact]
    public async Task SetSessionLimit_WithActiveSession_OverridesHoraLimiteWithoutModifyingQuota()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678", limiteSegundos: 10800));
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var sesion = TestDbContextFactory.SeedSesion(_context, TestDbContextFactory.CreateSesion(
            alumnoId: alumno.AlumnoID,
            equipoId: equipo.EquipoID));
        var newLimit = TimeHelper.GetPeruTime().AddMinutes(45);

        var result = await _controller.SetSessionLimit(new SetSessionLimitRequest { SesionId = sesion.SesionID, NuevaHoraLimite = newLimit });

        result.Should().BeOfType<OkObjectResult>();

        var reloadedSesion = await _context.Sesiones.FindAsync(sesion.SesionID);
        reloadedSesion!.HoraLimite.Should().BeCloseTo(newLimit, TimeSpan.FromSeconds(2));

        var reloadedAlumno = await _context.Alumnos.FindAsync(alumno.AlumnoID);
        reloadedAlumno!.LimiteDiarioSegundos.Should().Be(10800);
    }
}
