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
using Xunit;

public class AlumnosControllerBulkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AlumnosController _controller;

    public AlumnosControllerBulkTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _controller = new AlumnosController(_context, null!);
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
    public void Constructor_WithNullVisionService_InitializesSuccessfully()
    {
        var controller = new AlumnosController(_context, null!);

        controller.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkImport_WithNullList_Returns400BadRequestWithExpectedMessage()
    {
        var result = await _controller.BulkImport(null!);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Lista vacía");
    }

    [Fact]
    public async Task BulkImport_WithEmptyList_Returns400BadRequestWithExpectedMessage()
    {
        var result = await _controller.BulkImport(new List<Alumno>());

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Lista vacía");
    }

    [Fact]
    public async Task BulkImport_WithAllNewStudents_InsertsAllAndReturnsExpectedCounts()
    {
        var batch = new List<Alumno>
        {
            TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111", nombres: "Alumno Uno"),
            TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222", nombres: "Alumno Dos"),
            TestDbContextFactory.CreateAlumno(codigo: "20240003", dni: "33333333", nombres: "Alumno Tres")
        };

        var result = await _controller.BulkImport(batch);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("procesados").GetInt32().Should().Be(3);
        data.GetProperty("insertados").GetInt32().Should().Be(3);
        data.GetProperty("omitidos").GetInt32().Should().Be(0);

        _context.Alumnos.Count().Should().Be(3);
    }

    [Fact]
    public async Task BulkImport_WithAllExistingStudents_OmitsAllAndInsertsZero()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222"));

        var batch = new List<Alumno>
        {
            TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"),
            TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222")
        };

        var result = await _controller.BulkImport(batch);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("procesados").GetInt32().Should().Be(2);
        data.GetProperty("insertados").GetInt32().Should().Be(0);
        data.GetProperty("omitidos").GetInt32().Should().Be(2);

        _context.Alumnos.Count().Should().Be(2);
    }

    [Fact]
    public async Task BulkImport_WithMixedNewAndExistingStudents_InsertsOnlyNewAndOmitsDuplicates()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));

        var batch = new List<Alumno>
        {
            TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"),
            TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222"),
            TestDbContextFactory.CreateAlumno(codigo: "20240003", dni: "33333333")
        };

        var result = await _controller.BulkImport(batch);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("procesados").GetInt32().Should().Be(3);
        data.GetProperty("insertados").GetInt32().Should().Be(2);
        data.GetProperty("omitidos").GetInt32().Should().Be(1);

        _context.Alumnos.Count().Should().Be(3);
        _context.Alumnos.Any(a => a.CodigoUniversitario == "20240002").Should().BeTrue();
        _context.Alumnos.Any(a => a.CodigoUniversitario == "20240003").Should().BeTrue();
    }

    [Fact]
    public async Task BulkImport_WithWhitespaceData_TrimsAndPersistsSanitizedStrings()
    {
        var batch = new List<Alumno>
        {
            TestDbContextFactory.CreateAlumno(codigo: "  20240099  ", dni: "  99887766  ", nombres: "  Pedro  ")
        };

        var result = await _controller.BulkImport(batch);

        result.Should().BeOfType<OkObjectResult>();
        var reloaded = _context.Alumnos.FirstOrDefault(a => a.CodigoUniversitario == "20240099");
        reloaded.Should().NotBeNull();
        reloaded!.DNI.Should().Be("99887766");
        reloaded.Nombres.Should().Be("Pedro");
    }
}
