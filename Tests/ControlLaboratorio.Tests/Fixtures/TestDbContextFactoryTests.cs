namespace ControlLaboratorio.Tests.Fixtures;

using System;
using Xunit;
using FluentAssertions;
using ControlLaboratorio.API.Controllers;

public class TestDbContextFactoryTests : IDisposable
{
    public TestDbContextFactoryTests() => TestDbContextFactory.ResetActiveSessionPings();
    public void Dispose() => TestDbContextFactory.ResetActiveSessionPings();

    [Fact]
    public void CreateInMemoryDbContext_ShouldProvideIsolatedDatabaseInstance()
    {
        using var context1 = TestDbContextFactory.CreateInMemoryDbContext();
        using var context2 = TestDbContextFactory.CreateInMemoryDbContext();

        var alumno = TestDbContextFactory.CreateAlumno(codigo: "20240001", dni: "12345678");
        context1.Alumnos.Add(alumno);
        context1.SaveChanges();

        context1.Alumnos.Should().ContainSingle(a => a.CodigoUniversitario == "20240001");
        context2.Alumnos.Should().BeEmpty();
    }

    [Fact]
    public void SeedActiveEnvironment_ShouldPersistEntitiesAndClearTracker()
    {
        using var context = TestDbContextFactory.Create();
        var (alumno, equipo, sesion) = TestDbContextFactory.SeedActiveEnvironment(context, "20240002", "87654321", "PC-TEST");

        alumno.AlumnoID.Should().BeGreaterThan(0);
        equipo.EquipoID.Should().BeGreaterThan(0);
        sesion.SesionID.Should().BeGreaterThan(0);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void ResetActiveSessionPings_ShouldClearDictionary()
    {
        AuthController.ActiveSessionPings[99] = DateTime.UtcNow;
        AuthController.ActiveSessionPings.Should().ContainKey(99);

        TestDbContextFactory.ResetActiveSessionPings();
        AuthController.ActiveSessionPings.Should().BeEmpty();
    }
}
