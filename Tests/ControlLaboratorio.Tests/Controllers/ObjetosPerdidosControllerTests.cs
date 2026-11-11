namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class ObjetosPerdidosControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ObjetosPerdidosController _controller;

    public ObjetosPerdidosControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _controller = new ObjetosPerdidosController(_context);
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
    public async Task GetObjetosPerdidos_ReturnsItemsWithCalculatedDaysAndStatus()
    {
        var admin = new UsuarioAdmin
        {
            Username = "admin_test",
            PasswordHash = "hash",
            Nombres = "Admin Test",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);
        _context.SaveChanges();

        var itemCustodia = new ObjetoPerdido
        {
            Titulo = "Calculadora Casio",
            Categoria = CategoriaObjeto.Estudio,
            Descripcion = "Casio fx-991",
            LugarHallazgo = "Mesa 12",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-5),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}",
            FechaCreacion = DateTime.UtcNow
        };

        var itemVencido = new ObjetoPerdido
        {
            Titulo = "Audifonos Bluetooth",
            Categoria = CategoriaObjeto.Electronica,
            Descripcion = "Sony Negros",
            LugarHallazgo = "Laboratorio 2",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-35),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}",
            FechaCreacion = DateTime.UtcNow
        };

        var itemDevuelto = new ObjetoPerdido
        {
            Titulo = "Casaca Clinica",
            Categoria = CategoriaObjeto.Prendas,
            Descripcion = "Talla S blanca",
            LugarHallazgo = "Pasillo Principal",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-10),
            EstadoObjeto = EstadoObjeto.Devuelto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}",
            FechaHoraDevolucion = DateTime.UtcNow.AddDays(-1),
            CodigoURPReceptor = "20240001",
            DocumentoReceptor = "12345678",
            FechaCreacion = DateTime.UtcNow
        };

        var itemBaja = new ObjetoPerdido
        {
            Titulo = "Cuaderno Espiral",
            Categoria = CategoriaObjeto.Estudio,
            Descripcion = "Hojas recicladas",
            LugarHallazgo = "Mesa 5",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-40),
            EstadoObjeto = EstadoObjeto.Dispuesto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}",
            FechaBaja = DateTime.UtcNow.AddDays(-2),
            FechaCreacion = DateTime.UtcNow
        };

        _context.ObjetosPerdidos.AddRange(itemCustodia, itemVencido, itemDevuelto, itemBaja);
        _context.SaveChanges();

        var resultTodos = await _controller.GetAll(null, null, "todos");
        var okTodos = resultTodos.Should().BeOfType<OkObjectResult>().Subject;
        var listTodos = okTodos.Value.Should().BeOfType<List<ObjetoPerdidoDto>>().Subject;
        listTodos.Should().HaveCount(4);

        var dtoCustodia = listTodos.First(i => i.IdObjeto == itemCustodia.IdObjeto);
        dtoCustodia.DiasRestantes.Should().BeInRange(24, 25);
        dtoCustodia.EsVencido.Should().BeFalse();
        dtoCustodia.EstadoNombre.Should().Be("En Custodia");

        var dtoVencido = listTodos.First(i => i.IdObjeto == itemVencido.IdObjeto);
        dtoVencido.DiasRestantes.Should().Be(0);
        dtoVencido.EsVencido.Should().BeTrue();
        dtoVencido.EstadoNombre.Should().Be("Vencido");

        var dtoDevuelto = listTodos.First(i => i.IdObjeto == itemDevuelto.IdObjeto);
        dtoDevuelto.EstadoNombre.Should().Be("Devuelto");

        var dtoBaja = listTodos.First(i => i.IdObjeto == itemBaja.IdObjeto);
        dtoBaja.EstadoNombre.Should().Be("Dado de Baja");

        var resultCustodia = await _controller.GetAll(null, null, "custodia");
        var listCustodia = (resultCustodia as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listCustodia.Should().ContainSingle().Which.IdObjeto.Should().Be(itemCustodia.IdObjeto);

        var resultVencidos = await _controller.GetAll(null, null, "vencidos");
        var listVencidos = (resultVencidos as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listVencidos.Should().ContainSingle().Which.IdObjeto.Should().Be(itemVencido.IdObjeto);

        var resultDevueltos = await _controller.GetAll(null, null, "devueltos");
        var listDevueltos = (resultDevueltos as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listDevueltos.Should().ContainSingle().Which.IdObjeto.Should().Be(itemDevuelto.IdObjeto);

        var resultBajas = await _controller.GetAll(null, null, "bajas");
        var listBajas = (resultBajas as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listBajas.Should().ContainSingle().Which.IdObjeto.Should().Be(itemBaja.IdObjeto);

        var resultSearch = await _controller.GetAll("Casio", null, "todos");
        var listSearch = (resultSearch as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listSearch.Should().ContainSingle().Which.IdObjeto.Should().Be(itemCustodia.IdObjeto);

        var resultCategoria = await _controller.GetAll(null, (byte)CategoriaObjeto.Electronica, "todos");
        var listCategoria = (resultCategoria as OkObjectResult)!.Value as List<ObjetoPerdidoDto>;
        listCategoria.Should().ContainSingle().Which.IdObjeto.Should().Be(itemVencido.IdObjeto);
    }

    [Fact]
    public async Task GetKpis_CalculatesAccurateMetrics()
    {
        var admin = new UsuarioAdmin
        {
            Username = "kpi_admin",
            PasswordHash = "hash",
            Nombres = "KPI Admin",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);
        _context.SaveChanges();

        var custodia1 = new ObjetoPerdido
        {
            Titulo = "Mochila",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Azul",
            LugarHallazgo = "Pasillo",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-3),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };

        var custodia2 = new ObjetoPerdido
        {
            Titulo = "Lentes",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Con marco negro",
            LugarHallazgo = "Lab 1",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-15),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };

        var vencido = new ObjetoPerdido
        {
            Titulo = "Paraguas",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Negro",
            LugarHallazgo = "Entrada",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-32),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };

        var devuelto = new ObjetoPerdido
        {
            Titulo = "Billetera",
            Categoria = CategoriaObjeto.Documentos,
            Descripcion = "Marron",
            LugarHallazgo = "Lab 2",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-8),
            EstadoObjeto = EstadoObjeto.Devuelto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };

        var baja = new ObjetoPerdido
        {
            Titulo = "Gorra rota",
            Categoria = CategoriaObjeto.Prendas,
            Descripcion = "Deteriorada",
            LugarHallazgo = "Patio",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-40),
            EstadoObjeto = EstadoObjeto.Dispuesto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };

        _context.ObjetosPerdidos.AddRange(custodia1, custodia2, vencido, devuelto, baja);
        _context.SaveChanges();

        var result = await _controller.GetKpis();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var kpis = okResult.Value.Should().BeOfType<ObjetosPerdidosKpisDto>().Subject;

        kpis.TotalCustodia.Should().Be(2);
        kpis.TotalDevueltos.Should().Be(1);
        kpis.TotalVencidos.Should().Be(1);
        kpis.TotalBajas.Should().Be(1);
        kpis.TotalRegistrados.Should().Be(5);
    }

    [Fact]
    public async Task VerificarAlumno_WhenExists_ReturnsStudentDetails()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(
            codigo: "20241010",
            dni: "45678912",
            nombres: "Maria",
            apellidoPaterno: "Vargas",
            apellidoMaterno: "Salas",
            carrera: "Medicina Humana",
            correoInstitucional: "maria.vargas@urp.edu.pe"));

        var resultByCodigo = await _controller.VerificarAlumno("20241010");
        var okCodigo = resultByCodigo.Should().BeOfType<OkObjectResult>().Subject;
        var jsonCodigo = ToJsonElement(okCodigo.Value);

        jsonCodigo.GetProperty("existe").GetBoolean().Should().BeTrue();
        jsonCodigo.GetProperty("alumnoId").GetInt32().Should().Be(alumno.AlumnoID);
        jsonCodigo.GetProperty("codigoUniversitario").GetString().Should().Be("20241010");
        jsonCodigo.GetProperty("dni").GetString().Should().Be("45678912");
        jsonCodigo.GetProperty("nombres").GetString().Should().Be("Maria Vargas Salas");
        jsonCodigo.GetProperty("carrera").GetString().Should().Be("Medicina Humana");
        jsonCodigo.GetProperty("correoInstitucional").GetString().Should().Be("maria.vargas@urp.edu.pe");

        var resultByDni = await _controller.VerificarAlumno("45678912");
        var okDni = resultByDni.Should().BeOfType<OkObjectResult>().Subject;
        var jsonDni = ToJsonElement(okDni.Value);

        jsonDni.GetProperty("existe").GetBoolean().Should().BeTrue();
        jsonDni.GetProperty("alumnoId").GetInt32().Should().Be(alumno.AlumnoID);
    }

    [Fact]
    public async Task VerificarAlumno_WhenNotFound_ReturnsExisteFalse()
    {
        var result = await _controller.VerificarAlumno("99999999");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = ToJsonElement(okResult.Value);

        json.GetProperty("existe").GetBoolean().Should().BeFalse();
        json.GetProperty("message").GetString().Should().Be("Estudiante no figura en el sistema");
    }

    [Fact]
    public async Task RegistrarObjeto_ValidPayload_CreatesItemInCustodia()
    {
        var admin = new UsuarioAdmin
        {
            Username = "admin_hallazgo",
            PasswordHash = "hash123",
            Nombres = "Laura Quispe",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);
        _context.SaveChanges();

        var fechaHallazgo = DateTime.UtcNow.AddMinutes(-15);
        var dto = new RegistrarObjetoDto
        {
            Titulo = "Bata Medica Blanca",
            Categoria = CategoriaObjeto.Prendas,
            Descripcion = "Talla M bordado URP",
            LugarHallazgo = "Laboratorio 3 Piso 2",
            FechaHoraHallazgo = fechaHallazgo,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin
        };

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ObjetosPerdidosController.GetById));

        var responseDto = createdResult.Value.Should().BeOfType<ObjetoPerdidoDto>().Subject;
        responseDto.IdObjeto.Should().BeGreaterThan(0);
        responseDto.Titulo.Should().Be("Bata Medica Blanca");
        responseDto.Categoria.Should().Be(CategoriaObjeto.Prendas);
        responseDto.CategoriaNombre.Should().Be("Prendas");
        responseDto.EstadoObjeto.Should().Be(EstadoObjeto.EnCustodia);
        responseDto.EstadoNombre.Should().Be("En Custodia");
        responseDto.NombrePersonalHallazgo.Should().Be("admin_hallazgo • Laura Quispe");

        var itemInDb = await _context.ObjetosPerdidos.FindAsync(responseDto.IdObjeto);
        itemInDb.Should().NotBeNull();
        itemInDb!.Titulo.Should().Be("Bata Medica Blanca");
        itemInDb.EstadoObjeto.Should().Be(EstadoObjeto.EnCustodia);
        itemInDb.IdUsuarioHallazgo.Should().Be(admin.IdUsuarioAdmin);
        itemInDb.NombrePersonalHallazgo.Should().Be("admin_hallazgo • Laura Quispe");
    }

    [Fact]
    public async Task DevolverObjeto_Valid_ResolvesStudentAndSetsDevueltoStatus()
    {
        var alumno = TestDbContextFactory.SeedAlumno(_context, TestDbContextFactory.CreateAlumno(
            codigo: "20210005",
            dni: "70112233"));

        var admin = new UsuarioAdmin
        {
            Username = "entregador",
            PasswordHash = "hash456",
            Nombres = "Pedro Gomez",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Estetoscopio Littmann",
            Categoria = CategoriaObjeto.Estudio,
            Descripcion = "Color negro",
            LugarHallazgo = "Mesa 4",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-2),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}",
            FechaCreacion = DateTime.UtcNow
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var dto = new DevolverObjetoDto
        {
            CodigoUniversitario = "20210005",
            IdUsuarioEntrega = admin.IdUsuarioAdmin,
            Observaciones = "Entregado previa revision de identificacion"
        };

        var result = await _controller.Devolver(objeto.IdObjeto, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = ToJsonElement(okResult.Value);
        json.GetProperty("message").GetString().Should().Be("Objeto devuelto exitosamente.");

        var itemInDb = await _context.ObjetosPerdidos.FindAsync(objeto.IdObjeto);
        itemInDb.Should().NotBeNull();
        itemInDb!.EstadoObjeto.Should().Be(EstadoObjeto.Devuelto);
        itemInDb.FechaHoraDevolucion.Should().NotBeNull();
        itemInDb.AlumnoID.Should().Be(alumno.AlumnoID);
        itemInDb.DocumentoReceptor.Should().Be("70112233");
        itemInDb.CodigoURPReceptor.Should().Be("20210005");
        itemInDb.IdUsuarioEntrega.Should().Be(admin.IdUsuarioAdmin);
        itemInDb.NombrePersonalEntrega.Should().Be("entregador • Pedro Gomez");
        itemInDb.ObservacionesEntrega.Should().Be("Entregado previa revision de identificacion");
    }

    [Fact]
    public async Task DevolverObjeto_AlumnoNotFound_ReturnsNotFound()
    {
        var admin = new UsuarioAdmin
        {
            Username = "admin_dev",
            PasswordHash = "hash",
            Nombres = "Admin Dev",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Llaves de casillero",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Con llavero rojo",
            LugarHallazgo = "Bancas",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-1),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var dto = new DevolverObjetoDto
        {
            CodigoUniversitario = "99999999",
            IdUsuarioEntrega = admin.IdUsuarioAdmin
        };

        var result = await _controller.Devolver(objeto.IdObjeto, dto);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = ToJsonElement(notFoundResult.Value);
        json.GetProperty("message").GetString().Should().Be("El alumno no figura en el padrón institucional.");
    }

    [Fact]
    public async Task DevolverObjeto_ItemNotInCustodia_ReturnsBadRequest()
    {
        var admin = new UsuarioAdmin
        {
            Username = "admin_dev",
            PasswordHash = "hash",
            Nombres = "Admin Dev",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Mochila Negra",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Con libros",
            LugarHallazgo = "Pasillo",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-5),
            EstadoObjeto = EstadoObjeto.Devuelto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var dto = new DevolverObjetoDto
        {
            CodigoUniversitario = "20240001",
            IdUsuarioEntrega = admin.IdUsuarioAdmin
        };

        var result = await _controller.Devolver(objeto.IdObjeto, dto);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = ToJsonElement(badRequest.Value);
        json.GetProperty("message").GetString().Should().Be("Solo se pueden devolver objetos que se encuentren en custodia.");
    }

    [Fact]
    public async Task DarDeBaja_ValidItem_SetsDispuestoAndFechaBaja()
    {
        var admin = new UsuarioAdmin
        {
            Username = "baja_op",
            PasswordHash = "hash",
            Nombres = "Operador Baja",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Termo Metalico Roto",
            Categoria = CategoriaObjeto.Otros,
            Descripcion = "Muy golpeado",
            LugarHallazgo = "Cafeteria",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-45),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var result = await _controller.DarDeBaja(objeto.IdObjeto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = ToJsonElement(okResult.Value);
        json.GetProperty("message").GetString().Should().Be("Objeto dado de baja exitosamente.");

        var itemInDb = await _context.ObjetosPerdidos.FindAsync(objeto.IdObjeto);
        itemInDb.Should().NotBeNull();
        itemInDb!.EstadoObjeto.Should().Be(EstadoObjeto.Dispuesto);
        itemInDb.FechaBaja.Should().NotBeNull();
    }

    [Fact]
    public async Task DarDeBaja_ItemAlreadyDevuelto_ReturnsBadRequest()
    {
        var admin = new UsuarioAdmin
        {
            Username = "baja_op",
            PasswordHash = "hash",
            Nombres = "Operador Baja",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Tablet Samsung",
            Categoria = CategoriaObjeto.Electronica,
            Descripcion = "Gris",
            LugarHallazgo = "Lab 4",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-10),
            EstadoObjeto = EstadoObjeto.Devuelto,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var result = await _controller.DarDeBaja(objeto.IdObjeto);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = ToJsonElement(badRequest.Value);
        json.GetProperty("message").GetString().Should().Be("No se puede dar de baja un objeto que ya fue devuelto.");
    }

    [Fact]
    public async Task GetById_WhenItemExists_ReturnsItemDto()
    {
        var admin = new UsuarioAdmin
        {
            Username = "admin_get",
            PasswordHash = "hash",
            Nombres = "Admin Get",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true
        };
        _context.UsuariosAdmin.Add(admin);

        var objeto = new ObjetoPerdido
        {
            Titulo = "Libro de Anatomia",
            Categoria = CategoriaObjeto.Estudio,
            Descripcion = "Edicion 8",
            LugarHallazgo = "Biblioteca Central",
            FechaHoraHallazgo = DateTime.UtcNow.AddDays(-2),
            EstadoObjeto = EstadoObjeto.EnCustodia,
            IdUsuarioHallazgo = admin.IdUsuarioAdmin,
            NombrePersonalHallazgo = $"{admin.Username} • {admin.Nombres}"
        };
        _context.ObjetosPerdidos.Add(objeto);
        _context.SaveChanges();

        var result = await _controller.GetById(objeto.IdObjeto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<ObjetoPerdidoDto>().Subject;
        dto.IdObjeto.Should().Be(objeto.IdObjeto);
        dto.Titulo.Should().Be("Libro de Anatomia");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.GetById(9999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = ToJsonElement(notFoundResult.Value);
        json.GetProperty("message").GetString().Should().Be("Objeto perdido no encontrado.");
    }
}
