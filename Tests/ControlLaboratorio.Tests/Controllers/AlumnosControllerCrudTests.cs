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

public class AlumnosControllerCrudTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AlumnosController _controller;

    public AlumnosControllerCrudTests()
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
    public async Task GetAlumnos_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        var result = await _controller.GetAlumnos();

        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlumnos_WhenPopulated_ReturnsAllRecordsOrderedByAlumnoIdDescending()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "10000001"));
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "10000002"));
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240003", dni: "10000003"));

        var result = await _controller.GetAlumnos();

        result.Value.Should().NotBeNull();
        var list = result.Value!.ToList();
        list.Should().HaveCount(3);
        list.Should().BeInDescendingOrder(a => a.AlumnoID);
    }

    [Fact]
    public async Task GetAlumno_WhenStudentExists_Returns200WithAlumno()
    {
        var seeded = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240010", dni: "88887777", nombres: "Carlos"));

        var result = await _controller.GetAlumno(seeded.AlumnoID);

        result.Value.Should().NotBeNull();
        result.Value!.AlumnoID.Should().Be(seeded.AlumnoID);
        result.Value.CodigoUniversitario.Should().Be("20240010");
        result.Value.DNI.Should().Be("88887777");
    }

    [Fact]
    public async Task GetAlumno_WhenStudentDoesNotExist_Returns404NotFound()
    {
        var result = await _controller.GetAlumno(999);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PostAlumno_WithValidData_Returns201CreatedAtActionAndPersists()
    {
        var newAlumno = TestDbContextFactory.CreateAlumno(codigo: "20240020", dni: "77776666", nombres: "Ana");

        var result = await _controller.PostAlumno(newAlumno);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be("GetAlumno");
        var createdAlumno = createdResult.Value.Should().BeOfType<Alumno>().Subject;
        createdAlumno.AlumnoID.Should().BeGreaterThan(0);
        createdAlumno.CodigoUniversitario.Should().Be("20240020");

        var dbAlumno = await _context.Alumnos.FindAsync(createdAlumno.AlumnoID);
        dbAlumno.Should().NotBeNull();
        dbAlumno!.DNI.Should().Be("77776666");
    }

    [Fact]
    public async Task PostAlumno_WithDuplicateCodigoUniversitario_Returns409Conflict()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240030", dni: "11112222"));
        var duplicate = TestDbContextFactory.CreateAlumno(codigo: "20240030", dni: "33334444");

        var result = await _controller.PostAlumno(duplicate);

        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
        var data = ToJsonElement(conflictResult.Value);
        data.GetProperty("message").GetString().Should().Contain("20240030");
        _context.Alumnos.Count().Should().Be(1);
    }

    [Fact]
    public async Task PostAlumno_WithDuplicateDni_Returns409Conflict()
    {
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240040", dni: "55556666"));
        var duplicate = TestDbContextFactory.CreateAlumno(codigo: "20240041", dni: "55556666");

        var result = await _controller.PostAlumno(duplicate);

        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
        var data = ToJsonElement(conflictResult.Value);
        data.GetProperty("message").GetString().Should().Contain("55556666");
        _context.Alumnos.Count().Should().Be(1);
    }

    [Fact]
    public async Task PutAlumno_WithIdMismatch_Returns400BadRequest()
    {
        var alumno = TestDbContextFactory.CreateAlumno(id: 2, codigo: "20240001", dni: "12345678");

        var result = await _controller.PutAlumno(1, alumno);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task PutAlumno_WhenStudentDoesNotExist_Returns404NotFound()
    {
        var alumno = TestDbContextFactory.CreateAlumno(id: 999, codigo: "20249999", dni: "99999999");

        var result = await _controller.PutAlumno(999, alumno);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PutAlumno_WithDuplicateCodigoOnAnotherStudent_Returns409Conflict()
    {
        var student1 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222"));

        var updateData = TestDbContextFactory.CreateAlumno(id: student1.AlumnoID, codigo: "20240002", dni: "11111111");

        var result = await _controller.PutAlumno(student1.AlumnoID, updateData);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task PutAlumno_WithDuplicateDniOnAnotherStudent_Returns409Conflict()
    {
        var student1 = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));
        TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240002", dni: "22222222"));

        var updateData = TestDbContextFactory.CreateAlumno(id: student1.AlumnoID, codigo: "20240001", dni: "22222222");

        var result = await _controller.PutAlumno(student1.AlumnoID, updateData);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task PutAlumno_WhenUpdatingOwnDataWithSameCodeAndDni_Returns204NoContent()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111", nombres: "Original"));

        var updateData = TestDbContextFactory.CreateAlumno(id: student.AlumnoID, codigo: "20240001", dni: "11111111", nombres: "Modificado");

        var result = await _controller.PutAlumno(student.AlumnoID, updateData);

        result.Should().BeOfType<NoContentResult>();
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alumnos.FindAsync(student.AlumnoID);
        reloaded!.Nombres.Should().Be("Modificado");
    }

    [Fact]
    public async Task PutAlumno_WhenReactivatingInactiveStudent_AddsReactivationBonus()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111", estado: false, limiteSegundos: 10800));

        var updateData = TestDbContextFactory.CreateAlumno(id: student.AlumnoID, codigo: "20240001", dni: "11111111", estado: true, limiteSegundos: 10800);

        var result = await _controller.PutAlumno(student.AlumnoID, updateData);

        result.Should().BeOfType<NoContentResult>();
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alumnos.FindAsync(student.AlumnoID);
        reloaded!.Estado.Should().BeTrue();
        reloaded.LimiteDiarioSegundos.Should().Be(21600);
    }

    [Fact]
    public async Task PutAlumno_WhenReactivatingStudentWithDepletedQuota_NormalizesBaseAndAddsBonus()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111", estado: false, limiteSegundos: 0));

        var updateData = TestDbContextFactory.CreateAlumno(id: student.AlumnoID, codigo: "20240001", dni: "11111111", estado: true, limiteSegundos: 0);

        var result = await _controller.PutAlumno(student.AlumnoID, updateData);

        result.Should().BeOfType<NoContentResult>();
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alumnos.FindAsync(student.AlumnoID);
        reloaded!.LimiteDiarioSegundos.Should().Be(21600);
    }

    [Fact]
    public async Task PutAlumno_WhenUpdatingAlreadyActiveStudent_DoesNotAddBonus()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111", estado: true, limiteSegundos: 10800));

        var updateData = TestDbContextFactory.CreateAlumno(id: student.AlumnoID, codigo: "20240001", dni: "11111111", estado: true, limiteSegundos: 10800, nombres: "Updated");

        var result = await _controller.PutAlumno(student.AlumnoID, updateData);

        result.Should().BeOfType<NoContentResult>();
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alumnos.FindAsync(student.AlumnoID);
        reloaded!.LimiteDiarioSegundos.Should().Be(10800);
    }

    [Fact]
    public async Task DeleteAlumno_WhenStudentDoesNotExist_Returns404NotFound()
    {
        var result = await _controller.DeleteAlumno(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteAlumno_WhenStudentExists_RemovesFromDatabaseAndReturns204NoContent()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));

        var result = await _controller.DeleteAlumno(student.AlumnoID);

        result.Should().BeOfType<NoContentResult>();
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Alumnos.FindAsync(student.AlumnoID);
        reloaded.Should().BeNull();
        _context.Alumnos.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteAlumno_WhenCalledSequentially_SecondCallReturns404NotFound()
    {
        var student = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "11111111"));

        var firstResult = await _controller.DeleteAlumno(student.AlumnoID);
        var secondResult = await _controller.DeleteAlumno(student.AlumnoID);

        firstResult.Should().BeOfType<NoContentResult>();
        secondResult.Should().BeOfType<NotFoundResult>();
    }
}
