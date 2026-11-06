namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

[Collection("SequentialAuthTests")]
public class AuthControllerAdminLoginTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public AuthControllerAdminLoginTests()
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

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    private void SeedAdminUser(string username, string password, bool activo = true)
    {
        var user = new UsuarioAdmin
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 11),
            Nombres = "Administrador Prueba",
            Rol = RolAdmin.SuperAdmin,
            EstadoActivo = activo,
            FechaCreacion = DateTime.UtcNow
        };
        _context.UsuariosAdmin.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AdminLogin_WithValidCredentials_ReturnsOkWithToken()
    {
        SeedAdminUser("sysadmin", "secretPass99");
        var controller = new AuthController(_context, CreateConfig());

        var request = new AdminLoginRequest
        {
            Username = "sysadmin",
            Password = "secretPass99"
        };

        var result = await controller.AdminLogin(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AdminLoginResponse>().Subject;
        response.Token.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.EsLegacy.Should().BeFalse();
        response.Usuario.Username.Should().Be("sysadmin");
    }

    [Fact]
    public async Task AdminLogin_WithInvalidPassword_ReturnsUnauthorized()
    {
        SeedAdminUser("sysadmin", "secretPass99");
        var controller = new AuthController(_context, CreateConfig());

        var request = new AdminLoginRequest
        {
            Username = "sysadmin",
            Password = "wrongPassword"
        };

        var result = await controller.AdminLogin(request);

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Credenciales incorrectas.");
    }

    [Fact]
    public async Task AdminLogin_WithInvalidUsername_ReturnsUnauthorized()
    {
        SeedAdminUser("sysadmin", "secretPass99");
        var controller = new AuthController(_context, CreateConfig());

        var request = new AdminLoginRequest
        {
            Username = "wrongUser",
            Password = "secretPass99"
        };

        var result = await controller.AdminLogin(request);

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Credenciales incorrectas.");
    }

    [Fact]
    public async Task AdminLogin_WhenUserInactive_ReturnsUnauthorized()
    {
        SeedAdminUser("inactivo", "pass12345", activo: false);
        var controller = new AuthController(_context, CreateConfig());

        var request = new AdminLoginRequest
        {
            Username = "inactivo",
            Password = "pass12345"
        };

        var result = await controller.AdminLogin(request);

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Usuario inactivo o suspendido.");
    }

    [Fact]
    public async Task AdminLogin_WithTrimmedWhitespace_MatchesCredentials()
    {
        SeedAdminUser("admintest", "TestPassTrim99");
        var controller = new AuthController(_context, CreateConfig());

        var request = new AdminLoginRequest
        {
            Username = "  admintest  ",
            Password = "  TestPassTrim99  "
        };

        var result = await controller.AdminLogin(request);

        result.Should().BeOfType<OkObjectResult>();
    }
}
