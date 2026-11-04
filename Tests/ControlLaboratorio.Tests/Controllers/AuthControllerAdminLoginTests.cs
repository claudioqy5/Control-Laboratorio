namespace ControlLaboratorio.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
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

    private static IConfiguration CreateConfig(string? username = "admin", string? password = "admin123")
    {
        var dict = new Dictionary<string, string?>();
        if (username != null) dict["AdminSettings:Username"] = username;
        if (password != null) dict["AdminSettings:Password"] = password;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static JsonElement ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void AdminLogin_WithValidCredentials_ReturnsOkWithToken()
    {
        var config = CreateConfig("sysadmin", "secretPass99");
        var controller = new AuthController(_context, config);

        var request = new AdminLoginRequest
        {
            Username = "sysadmin",
            Password = "secretPass99"
        };

        var result = controller.AdminLogin(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("token").GetString().Should().Be("fake-jwt-token-for-now");
        data.GetProperty("message").GetString().Should().Be("Bienvenido Administrador");
    }

    [Fact]
    public void AdminLogin_WithInvalidPassword_ReturnsUnauthorized()
    {
        var config = CreateConfig("sysadmin", "secretPass99");
        var controller = new AuthController(_context, config);

        var request = new AdminLoginRequest
        {
            Username = "sysadmin",
            Password = "wrongPassword"
        };

        var result = controller.AdminLogin(request);

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public void AdminLogin_WithInvalidUsername_ReturnsUnauthorized()
    {
        var config = CreateConfig("sysadmin", "secretPass99");
        var controller = new AuthController(_context, config);

        var request = new AdminLoginRequest
        {
            Username = "wrongUser",
            Password = "secretPass99"
        };

        var result = controller.AdminLogin(request);

        var unauthResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var data = ToJsonElement(unauthResult.Value);
        data.GetProperty("message").GetString().Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public void AdminLogin_WhenConfigMissing_FallsBackToDefaultCredentials()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new AuthController(_context, config);

        var request = new AdminLoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        var result = controller.AdminLogin(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ToJsonElement(okResult.Value);
        data.GetProperty("token").GetString().Should().Be("fake-jwt-token-for-now");
    }

    [Fact]
    public void AdminLogin_WithTrimmedWhitespace_MatchesCredentials()
    {
        var config = CreateConfig("admin", "admin123");
        var controller = new AuthController(_context, config);

        var request = new AdminLoginRequest
        {
            Username = "  admin  ",
            Password = "  admin123  "
        };

        var result = controller.AdminLogin(request);

        result.Should().BeOfType<OkObjectResult>();
    }
}
