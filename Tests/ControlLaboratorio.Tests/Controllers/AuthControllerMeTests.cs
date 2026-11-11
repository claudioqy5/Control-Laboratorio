namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

[Collection("SequentialAuthTests")]
public class AuthControllerMeTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public AuthControllerMeTests()
    {
        TestDbContextFactory.ResetActiveSessionPings();
        _context = TestDbContextFactory.CreateInMemoryDbContext();
    }

    public void Dispose()
    {
        TestDbContextFactory.ResetActiveSessionPings();
        _context.Dispose();
    }

    private static IConfiguration CreateConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64))
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static void SetUserContext(ControllerBase controller, string? userIdClaim, string claimType = ClaimTypes.NameIdentifier)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            claims.Add(new Claim(claimType, userIdClaim));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private UsuarioAdmin SeedAdminUser(string username, string nombres, RolAdmin rol = RolAdmin.SuperAdmin, bool activo = true)
    {
        var user = new UsuarioAdmin
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123", 11),
            Nombres = nombres,
            Correo = $"{username}@urp.edu.pe",
            Rol = rol,
            EstadoActivo = activo,
            FechaCreacion = DateTime.UtcNow
        };
        _context.UsuariosAdmin.Add(user);
        _context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticatedAndActive_ReturnsOkWithUserDto()
    {
        var user = SeedAdminUser("jvaldivia", "Juan Valdivia", RolAdmin.SuperAdmin, activo: true);
        var controller = new AuthController(_context, CreateConfig());
        SetUserContext(controller, user.IdUsuarioAdmin.ToString(), ClaimTypes.NameIdentifier);

        var result = await controller.GetCurrentUser();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<UsuarioAdminDto>().Subject;
        dto.IdUsuarioAdmin.Should().Be(user.IdUsuarioAdmin);
        dto.Username.Should().Be("jvaldivia");
        dto.Nombres.Should().Be("Juan Valdivia");
        dto.Correo.Should().Be("jvaldivia@urp.edu.pe");
        dto.Rol.Should().Be(RolAdmin.SuperAdmin);
        dto.RolNombre.Should().Be("SuperAdmin");
        dto.EstadoActivo.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUser_WhenClaimIsSub_ReturnsOkWithUserDto()
    {
        var user = SeedAdminUser("mgomez", "Maria Gomez", RolAdmin.Laboratorio, activo: true);
        var controller = new AuthController(_context, CreateConfig());
        SetUserContext(controller, user.IdUsuarioAdmin.ToString(), "sub");

        var result = await controller.GetCurrentUser();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<UsuarioAdminDto>().Subject;
        dto.IdUsuarioAdmin.Should().Be(user.IdUsuarioAdmin);
        dto.Username.Should().Be("mgomez");
        dto.Rol.Should().Be(RolAdmin.Laboratorio);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNotFound_ReturnsUnauthorized()
    {
        var controller = new AuthController(_context, CreateConfig());
        SetUserContext(controller, "99999", ClaimTypes.NameIdentifier);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserIsInactive_ReturnsUnauthorized()
    {
        var user = SeedAdminUser("inactivo_admin", "Inactivo Admin", RolAdmin.Biblioteca, activo: false);
        var controller = new AuthController(_context, CreateConfig());
        SetUserContext(controller, user.IdUsuarioAdmin.ToString(), ClaimTypes.NameIdentifier);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenClaimMissingOrNonNumeric_ReturnsUnauthorized()
    {
        var controller = new AuthController(_context, CreateConfig());
        SetUserContext(controller, null);

        var result = await controller.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedResult>();

        SetUserContext(controller, "not-a-number");
        var resultInvalid = await controller.GetCurrentUser();
        resultInvalid.Should().BeOfType<UnauthorizedResult>();
    }
}
