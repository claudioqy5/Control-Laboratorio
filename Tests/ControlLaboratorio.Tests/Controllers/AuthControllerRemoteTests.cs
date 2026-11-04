namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

[Collection("SequentialAuthTests")]
public class AuthControllerRemoteTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerRemoteTests()
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
    public async Task TriggerRemoteUnlock_WhenEquipoDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.TriggerRemoteUnlock("PC-NON-EXISTENT");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TriggerRemoteUnlock_WhenEquipoExists_CreatesAdminAndSession_EnqueuesUnlock()
    {
        var pcName = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pcName));

        var result = await _controller.TriggerRemoteUnlock(pcName);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        var sesionId = data.GetProperty("sesionId").GetInt32();
        sesionId.Should().BeGreaterThan(0);

        var checkResult = _controller.CheckRemoteUnlock(pcName);
        var checkOk = checkResult.Should().BeOfType<OkObjectResult>().Subject;
        var checkData = ToJsonElement(checkOk.Value);
        checkData.GetProperty("unlock").GetBoolean().Should().BeTrue();
        checkData.GetProperty("sesionId").GetInt32().Should().Be(sesionId);
        checkData.GetProperty("shutdown").GetBoolean().Should().BeFalse();

        var checkAgain = _controller.CheckRemoteUnlock(pcName);
        var againData = ToJsonElement(checkAgain.Should().BeOfType<OkObjectResult>().Subject.Value);
        againData.GetProperty("unlock").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRemoteUnlock_WhenAdminAlreadyExists_ReusesExistingAdmin()
    {
        var pcName = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "ADMIN", dni: "00000000", nombres: "ADMINISTRADOR"));
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pcName));

        var result = await _controller.TriggerRemoteUnlock(pcName);

        result.Should().BeOfType<OkObjectResult>();
        _context.Alumnos.Count(a => a.CodigoUniversitario == "ADMIN").Should().Be(1);
    }

    [Fact]
    public async Task TriggerRemoteUnlockAll_DefaultOnlyFreeFalse_UnlocksAllAndClosesActiveSession()
    {
        var pcActive = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        var pcFree = $"PC-{Guid.NewGuid():N}".Substring(0, 15);

        var (_, _, sesionActive) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240099", "11223344", pcActive);
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pcFree));

        var result = await _controller.TriggerRemoteUnlockAll(onlyFree: false);

        result.Should().BeOfType<OkObjectResult>();

        var reloadedSesion = await _context.Sesiones.FindAsync(sesionActive.SesionID);
        reloadedSesion!.HoraFin.Should().NotBeNull();

        var checkActive = ToJsonElement(_controller.CheckRemoteUnlock(pcActive).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkActive.GetProperty("unlock").GetBoolean().Should().BeTrue();

        var checkFree = ToJsonElement(_controller.CheckRemoteUnlock(pcFree).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkFree.GetProperty("unlock").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TriggerRemoteUnlockAll_WithOnlyFreeTrue_SkipsOccupiedEquipment()
    {
        var pcActive = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        var pcFree = $"PC-{Guid.NewGuid():N}".Substring(0, 15);

        var (_, _, sesionActive) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240098", "99887766", pcActive);
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pcFree));

        var result = await _controller.TriggerRemoteUnlockAll(onlyFree: true);

        result.Should().BeOfType<OkObjectResult>();

        var reloadedSesion = await _context.Sesiones.FindAsync(sesionActive.SesionID);
        reloadedSesion!.HoraFin.Should().BeNull();

        var checkActive = ToJsonElement(_controller.CheckRemoteUnlock(pcActive).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkActive.GetProperty("unlock").GetBoolean().Should().BeFalse();

        var checkFree = ToJsonElement(_controller.CheckRemoteUnlock(pcFree).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkFree.GetProperty("unlock").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TriggerRemoteShutdown_WhenEquipoDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.TriggerRemoteShutdown("PC-NOT-FOUND");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TriggerRemoteShutdown_WhenEquipoExists_ClosesActiveSessionAndEnqueuesShutdown()
    {
        var pcName = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        var (_, _, sesion) = TestDbContextFactory.SeedActiveEnvironment(_context, "20240097", "55443322", pcName);

        var result = await _controller.TriggerRemoteShutdown(pcName);

        result.Should().BeOfType<OkObjectResult>();

        var reloadedSesion = await _context.Sesiones.FindAsync(sesion.SesionID);
        reloadedSesion!.HoraFin.Should().NotBeNull();

        var checkFirst = ToJsonElement(_controller.CheckRemoteShutdown(pcName).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkFirst.GetProperty("shutdown").GetBoolean().Should().BeTrue();

        var checkSecond = ToJsonElement(_controller.CheckRemoteShutdown(pcName).Should().BeOfType<OkObjectResult>().Subject.Value);
        checkSecond.GetProperty("shutdown").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task TriggerRemoteShutdownAll_EnqueuesShutdownForAllEquipos()
    {
        var pc1 = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        var pc2 = $"PC-{Guid.NewGuid():N}".Substring(0, 15);

        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pc1));
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pc2));

        var result = await _controller.TriggerRemoteShutdownAll();

        result.Should().BeOfType<OkObjectResult>();

        var check1 = ToJsonElement(_controller.CheckRemoteShutdown(pc1).Should().BeOfType<OkObjectResult>().Subject.Value);
        check1.GetProperty("shutdown").GetBoolean().Should().BeTrue();

        var check2 = ToJsonElement(_controller.CheckRemoteShutdown(pc2).Should().BeOfType<OkObjectResult>().Subject.Value);
        check2.GetProperty("shutdown").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CheckRemoteUnlock_WhenUnlockAndShutdownBothPending_ReturnsBothAndDrains()
    {
        var pcName = $"PC-{Guid.NewGuid():N}".Substring(0, 15);
        TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: pcName));

        await _controller.TriggerRemoteUnlock(pcName);
        await _controller.TriggerRemoteShutdown(pcName);

        var result = _controller.CheckRemoteUnlock(pcName);
        var data = ToJsonElement(result.Should().BeOfType<OkObjectResult>().Subject.Value);
        data.GetProperty("unlock").GetBoolean().Should().BeTrue();
        data.GetProperty("shutdown").GetBoolean().Should().BeTrue();

        var drained = ToJsonElement(_controller.CheckRemoteUnlock(pcName).Should().BeOfType<OkObjectResult>().Subject.Value);
        drained.GetProperty("unlock").GetBoolean().Should().BeFalse();
        drained.GetProperty("shutdown").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void CheckRemoteShutdown_WhenNoPendingCommand_ReturnsFalse()
    {
        var pcName = $"PC-{Guid.NewGuid():N}".Substring(0, 15);

        var result = _controller.CheckRemoteShutdown(pcName);
        var data = ToJsonElement(result.Should().BeOfType<OkObjectResult>().Subject.Value);

        data.GetProperty("shutdown").GetBoolean().Should().BeFalse();
    }
}
