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

public class StatsControllerSecurityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly StatsController _controller;

    public StatsControllerSecurityTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AdminSettings:ActionPassword", "secureAction999" }
            })
            .Build();
        _controller = new StatsController(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task AssignMapSlot_WithWrongPassword_ReturnsUnauthorized()
    {
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var request = new AssignMapSlotRequest
        {
            EquipoID = equipo.EquipoID,
            PosicionMapa = 5,
            Password = "incorrectPassword"
        };

        var result = await _controller.AssignMapSlot(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AssignMapSlot_WithCorrectPassword_UpdatesPositionAndClearsDuplicateSlot()
    {
        var pc1 = TestDbContextFactory.SeedEquipo(_context, new Equipo { NombreRed = "PC-01", PosicionMapa = 5, Estado = true });
        var pc2 = TestDbContextFactory.SeedEquipo(_context, new Equipo { NombreRed = "PC-02", PosicionMapa = null, Estado = true });

        var request = new AssignMapSlotRequest
        {
            EquipoID = pc2.EquipoID,
            PosicionMapa = 5,
            Password = "secureAction999"
        };

        var result = await _controller.AssignMapSlot(request);

        result.Should().BeOfType<OkObjectResult>();

        var updatedPc1 = await _context.Equipos.FindAsync(pc1.EquipoID);
        var updatedPc2 = await _context.Equipos.FindAsync(pc2.EquipoID);

        updatedPc1!.PosicionMapa.Should().BeNull();
        updatedPc2!.PosicionMapa.Should().Be(5);
    }

    [Fact]
    public async Task AssignMapSlot_WhenEquipmentNotFound_ReturnsNotFound()
    {
        var request = new AssignMapSlotRequest
        {
            EquipoID = 9999,
            PosicionMapa = 1,
            Password = "secureAction999"
        };

        var result = await _controller.AssignMapSlot(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetAlias_WithWrongPassword_ReturnsUnauthorized()
    {
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var request = new SetAliasRequest
        {
            EquipoID = equipo.EquipoID,
            Alias = "Laboratorio Alfa",
            Password = "wrongPassword"
        };

        var result = await _controller.SetAlias(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SetAlias_WithCorrectPassword_UpdatesAliasSuccessfully()
    {
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var request = new SetAliasRequest
        {
            EquipoID = equipo.EquipoID,
            Alias = "Laboratorio Alfa",
            Password = "secureAction999"
        };

        var result = await _controller.SetAlias(request);

        result.Should().BeOfType<OkObjectResult>();
        var updated = await _context.Equipos.FindAsync(equipo.EquipoID);
        updated!.Alias.Should().Be("Laboratorio Alfa");
    }

    [Fact]
    public async Task SetComentario_WithWrongPassword_ReturnsUnauthorized()
    {
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var request = new SetComentarioRequest
        {
            EquipoID = equipo.EquipoID,
            Comentario = "Equipo en mantenimiento preventivo",
            Password = "wrongPassword"
        };

        var result = await _controller.SetComentario(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SetComentario_WithCorrectPassword_UpdatesComentarioSuccessfully()
    {
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));

        var request = new SetComentarioRequest
        {
            EquipoID = equipo.EquipoID,
            Comentario = "Equipo en mantenimiento preventivo",
            Password = "secureAction999"
        };

        var result = await _controller.SetComentario(request);

        result.Should().BeOfType<OkObjectResult>();
        var updated = await _context.Equipos.FindAsync(equipo.EquipoID);
        updated!.Comentario.Should().Be("Equipo en mantenimiento preventivo");
    }

    [Fact]
    public async Task GetUsageStats_ReturnsSessionsGroupedByHour()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno());
        var equipo = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo());

        var today = DateTime.Today;
        _context.Sesiones.AddRange(
            new Sesion { AlumnoID = alumno.AlumnoID, EquipoID = equipo.EquipoID, Fecha = today, HoraInicio = today.AddHours(9), HoraFin = today.AddHours(10) },
            new Sesion { AlumnoID = alumno.AlumnoID, EquipoID = equipo.EquipoID, Fecha = today, HoraInicio = today.AddHours(9).AddMinutes(15), HoraFin = today.AddHours(10) },
            new Sesion { AlumnoID = alumno.AlumnoID, EquipoID = equipo.EquipoID, Fecha = today, HoraInicio = today.AddHours(14), HoraFin = today.AddHours(15) }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetUsageStats();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var element = ToJsonElement(okResult.Value);
        element.GetArrayLength().Should().Be(2);

        var firstHour = element[0];
        firstHour.GetProperty("Hora").GetInt32().Should().Be(9);
        firstHour.GetProperty("Cantidad").GetInt32().Should().Be(2);

        var secondHour = element[1];
        secondHour.GetProperty("Hora").GetInt32().Should().Be(14);
        secondHour.GetProperty("Cantidad").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetActiveSessions_ReturnsOnlySessionsWithNullHoraFin()
    {
        var alumno1 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));
        var alumno2 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222"));
        var equipo1 = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-01"));
        var equipo2 = TestDbContextFactory.SeedEquipo(_context, TestDbContextFactory.CreateEquipo(nombreRed: "PC-02"));

        var today = DateTime.Today;
        _context.Sesiones.AddRange(
            new Sesion { AlumnoID = alumno1.AlumnoID, EquipoID = equipo1.EquipoID, Fecha = today, HoraInicio = today.AddHours(10), HoraFin = null },
            new Sesion { AlumnoID = alumno2.AlumnoID, EquipoID = equipo2.EquipoID, Fecha = today, HoraInicio = today.AddHours(8), HoraFin = today.AddHours(9) }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.GetActiveSessions();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var element = ToJsonElement(okResult.Value);
        element.GetArrayLength().Should().Be(1);
        element[0].GetProperty("Equipo").GetString().Should().Be("PC-01");
    }
}
