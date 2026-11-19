using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Controllers;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Security;
using ProyectoVet.Tests.Support;
using Xunit;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class AuthControllerTests
{
    private static VeterinariaContext CreateContext() => new(
        new DbContextOptionsBuilder<VeterinariaContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedEmployeeAsync(VeterinariaContext db, string role, bool active = true)
    {
        db.Empleados.Add(new Empleado
        {
            IdEmpleado = 7, Nombre = "Ana", Apellidos = "Prueba", NumeroCedula = "TEST7",
            Telefono = "88888888", CorreoElectronico = "ana@test.local", Contrasena = "Prueba123!",
            Activo = active, IdRolNavigation = new Role { NombreRol = role }
        });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("Administrador", "Administrador")]
    [InlineData("Veterinario", "Veterinario")]
    [InlineData("Secretaria", "Secretaria")]
    [InlineData("Recepcionista", "Secretaria")]
    public async Task Login_CredencialesValidas_AutenticaConIdentidadYRolCorrectos(string role, string expectedRole)
    {
        await using var db = CreateContext();
        await SeedEmployeeAsync(db, role);
        using var test = new ControllerTestScope<AuthController>(new AuthController(db));

        var result = Assert.IsType<RedirectToActionResult>(await test.Controller.Login("ana@test.local", "Prueba123!"));

        Assert.Equal("Home", result.ControllerName);
        Assert.Equal("Index", result.ActionName);
        var principal = Assert.IsType<ClaimsPrincipal>(test.Authentication.SignedInPrincipal);
        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Equal("7", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Ana Prueba", principal.Identity.Name);
        Assert.Equal(expectedRole, principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, test.Authentication.SignInScheme);
        Assert.Equal("7", test.Controller.HttpContext.Session.GetString("EmpleadoId"));
        Assert.Equal("Ana Prueba", test.Controller.HttpContext.Session.GetString("EmpleadoNombre"));
        Assert.DoesNotContain(principal.Claims, c => c.Value == "Prueba123!");
    }

    [Theory]
    [InlineData("", "Prueba123!")]
    [InlineData("ana@test.local", "")]
    [InlineData(" ", " ")]
    [InlineData("otro@test.local", "Prueba123!")]
    [InlineData("ana@test.local", "Incorrecta")]
    public async Task Login_DatosInvalidos_NoAutenticaNiCreaSesion(string email, string password)
    {
        await using var db = CreateContext();
        await SeedEmployeeAsync(db, RoleAccess.Veterinario);
        using var test = new ControllerTestScope<AuthController>(new AuthController(db));

        Assert.IsType<ViewResult>(await test.Controller.Login(email, password));

        Assert.False(test.Controller.ModelState.IsValid);
        Assert.Null(test.Authentication.SignedInPrincipal);
        Assert.Empty(test.Controller.HttpContext.Session.Keys);
    }

    [Theory]
    [InlineData("Veterinario", false)]
    [InlineData("RolDesconocido", true)]
    public async Task Login_CuentaSinAcceso_NoAutentica(string role, bool active)
    {
        await using var db = CreateContext();
        await SeedEmployeeAsync(db, role, active);
        using var test = new ControllerTestScope<AuthController>(new AuthController(db));

        Assert.IsType<ViewResult>(await test.Controller.Login("ana@test.local", "Prueba123!"));
        Assert.False(test.Controller.ModelState.IsValid);
        Assert.Null(test.Authentication.SignedInPrincipal);
        Assert.Empty(test.Controller.HttpContext.Session.Keys);
    }

    [Fact]
    public void LoginGet_Anonimo_MuestraFormulario()
    {
        using var db = CreateContext();
        using var test = new ControllerTestScope<AuthController>(new AuthController(db));
        Assert.IsType<ViewResult>(test.Controller.Login());
    }

    [Fact]
    public void LoginGet_Autenticado_RedirigeAlPanel()
    {
        using var db = CreateContext();
        using var test = new ControllerTestScope<AuthController>(new AuthController(db), RoleAccess.Secretaria);
        var result = Assert.IsType<RedirectToActionResult>(test.Controller.Login());
        Assert.Equal("Home", result.ControllerName);
        Assert.Equal("Index", result.ActionName);
    }

    [Fact]
    public async Task Logout_EliminaAutenticacionYLimpiaSesion()
    {
        await using var db = CreateContext();
        using var test = new ControllerTestScope<AuthController>(new AuthController(db), RoleAccess.Veterinario);
        test.Controller.HttpContext.Session.SetString("EmpleadoId", "7");
        test.Controller.HttpContext.Session.SetString("EmpleadoRol", RoleAccess.Veterinario);

        var result = Assert.IsType<RedirectToActionResult>(await test.Controller.Logout());

        Assert.Equal("Login", result.ActionName);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, test.Authentication.SignOutScheme);
        Assert.Empty(test.Controller.HttpContext.Session.Keys);
    }

    [Fact]
    public void AccessDenied_RespondeCon403()
    {
        using var db = CreateContext();
        using var test = new ControllerTestScope<AuthController>(new AuthController(db), RoleAccess.Secretaria);
        Assert.IsType<ViewResult>(test.Controller.AccessDenied());
        Assert.Equal(StatusCodes.Status403Forbidden, test.Controller.Response.StatusCode);
    }
}
