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
public class UsuariosAdminControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public UsuariosAdminControllerTests()
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

    private static void SetUserContext(ControllerBase controller, string userId, string username, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task CreateUsuario_WhenDatabaseEmpty_AllowsSuperAdminSeedWithoutAuth()
    {
        var controller = new UsuariosAdminController(_context);

        var request = new CreateUsuarioAdminRequest
        {
            Username = "fvalero",
            Password = "SecurePassword123!",
            Nombres = "Dr. Fernando Valero",
            Correo = "fvalero@urp.edu.pe",
            Rol = RolAdmin.SuperAdmin
        };

        var result = await controller.CreateUsuario(request);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<UsuarioAdminDto>().Subject;
        dto.Username.Should().Be("fvalero");
        dto.Rol.Should().Be(RolAdmin.SuperAdmin);
        dto.EstadoActivo.Should().BeTrue();

        var userInDb = await _context.UsuariosAdmin.FindAsync(dto.IdUsuarioAdmin);
        userInDb.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("SecurePassword123!", userInDb!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUsuario_WhenDuplicateUsername_ReturnsConflict()
    {
        var controller = new UsuariosAdminController(_context);
        SetUserContext(controller, "1", "admin", "SuperAdmin");

        var request1 = new CreateUsuarioAdminRequest
        {
            Username = "bibliotecario1",
            Password = "Password123!",
            Nombres = "Bibliotecario Uno",
            Rol = RolAdmin.Biblioteca
        };
        await controller.CreateUsuario(request1);

        var requestDuplicate = new CreateUsuarioAdminRequest
        {
            Username = "BIBLIOTECARIO1",
            Password = "Password456!",
            Nombres = "Bibliotecario Dos",
            Rol = RolAdmin.Biblioteca
        };

        var result = await controller.CreateUsuario(requestDuplicate);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AdminLogin_WithDatabaseUser_ReturnsOkWithTokenAndRtr()
    {
        var user = new UsuarioAdmin
        {
            Username = "operador",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("LabPassword2026", 11),
            Nombres = "Operador Lab",
            Rol = RolAdmin.Laboratorio,
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.UsuariosAdmin.Add(user);
        await _context.SaveChangesAsync();

        var authController = new AuthController(_context, CreateConfig());
        authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var loginRequest = new AdminLoginRequest
        {
            Username = "operador",
            Password = "LabPassword2026"
        };

        var result = await authController.AdminLogin(loginRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AdminLoginResponse>().Subject;
        response.Token.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.EsLegacy.Should().BeFalse();
        response.Usuario.Username.Should().Be("operador");
        response.Usuario.Rol.Should().Be(RolAdmin.Laboratorio);
    }

    [Fact]
    public async Task RefreshToken_RotatesSuccessfully_AndInvalidatesOldToken()
    {
        var user = new UsuarioAdmin
        {
            Username = "superadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123", 11),
            Nombres = "Super Admin",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.UsuariosAdmin.Add(user);
        await _context.SaveChangesAsync();

        var initialToken = new RefreshToken
        {
            IdUsuarioAdmin = user.IdUsuarioAdmin,
            Token = "valid-refresh-token-12345",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(initialToken);
        await _context.SaveChangesAsync();

        var authController = new AuthController(_context, CreateConfig());
        authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token-12345"
        };

        var result = await authController.RefreshToken(refreshRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AdminLoginResponse>().Subject;
        response.Token.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBe("valid-refresh-token-12345");

        var oldTokenInDb = await _context.RefreshTokens.FindAsync(initialToken.Id);
        oldTokenInDb!.RevokedAt.Should().NotBeNull();
        oldTokenInDb.ReplacedByToken.Should().Be(response.RefreshToken);
    }

    [Fact]
    public async Task ToggleEstado_DeactivatesUser_AndRevokesAllTokens()
    {
        var user = new UsuarioAdmin
        {
            Username = "user_to_block",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123", 11),
            Nombres = "Usuario Bloqueable",
            Rol = RolAdmin.Biblioteca,
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.UsuariosAdmin.Add(user);
        await _context.SaveChangesAsync();

        var token = new RefreshToken
        {
            IdUsuarioAdmin = user.IdUsuarioAdmin,
            Token = "active-token-to-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        var controller = new UsuariosAdminController(_context);
        SetUserContext(controller, "999", "another_superadmin", "SuperAdmin");

        var result = await controller.ToggleEstado(user.IdUsuarioAdmin);

        result.Should().BeOfType<OkObjectResult>();

        var userInDb = await _context.UsuariosAdmin.FindAsync(user.IdUsuarioAdmin);
        userInDb!.EstadoActivo.Should().BeFalse();

        var tokenInDb = await _context.RefreshTokens.FindAsync(token.Id);
        tokenInDb!.RevokedAt.Should().NotBeNull();
    }
}
