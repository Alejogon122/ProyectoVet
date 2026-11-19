using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ProyectoVet.Security;
using Xunit;

namespace ProyectoVet.Tests;

[Trait("Category", "Unit")]
public class RoleAccessTests
{
    // Casos de uso del producto y límites de cada perfil, incluidos recursos nuevos/desconocidos.
    [Theory]
    [InlineData("Administrador", "Empleadoes", "Create", true)]
    [InlineData("Administrador", "Consultas", "Delete", true)]
    [InlineData("Administrador", "ModuloDesconocido", "Index", false)]
    [InlineData("Administrador", "Consultas", "Exportar", false)]
    [InlineData("Veterinario", "Consultas", "Create", true)]
    [InlineData("Veterinario", "Consultas", "Edit", true)]
    [InlineData("Veterinario", "Consultas", "Delete", false)]
    [InlineData("Veterinario", "ExpedientesClinicoes", "Edit", true)]
    [InlineData("Veterinario", "ExpedientesClinicoes", "Delete", false)]
    [InlineData("Veterinario", "Citas", "Details", true)]
    [InlineData("Veterinario", "Citas", "Create", false)]
    [InlineData("Veterinario", "Mascotas", "Edit", false)]
    [InlineData("Veterinario", "Medicamentos", "Index", true)]
    [InlineData("Veterinario", "Medicamentos", "Edit", false)]
    [InlineData("Veterinario", "Permisoes", "Index", false)]
    [InlineData("Secretaria", "Dueno", "Create", true)]
    [InlineData("Secretaria", "Mascotas", "Edit", true)]
    [InlineData("Secretaria", "Citas", "Delete", true)]
    [InlineData("Secretaria", "Consultas", "Details", false)]
    [InlineData("Secretaria", "ExpedientesClinicoes", "Index", false)]
    [InlineData("Secretaria", "Empleadoes", "Edit", false)]
    [InlineData("Recepcionista", "Citas", "Create", true)]
    [InlineData("Recepcionista", "Consultas", "Create", false)]
    [InlineData("RolDesconocido", "Home", "Index", false)]
    [InlineData("", "Mascotas", "Index", false)]
    public void Acceso_RespetaLosLimitesDelPerfil(string role, string controller, string action, bool allowed)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Prueba"));
        Assert.Equal(allowed, RoleAccess.CanAccess(user, controller, action));
    }

    [Fact]
    public void RolSinIdentidadAutenticada_NoOtorgaAcceso()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, RoleAccess.Administrador)]));
        Assert.False(RoleAccess.CanAccess(user, "Empleadoes", "Create"));
        Assert.True(RoleAccess.CanAccess(user, "Auth", "Login"));
        Assert.True(RoleAccess.CanAccess(user, "Auth", "AccessDenied"));
        Assert.False(RoleAccess.CanAccess(user, "Auth", "Logout"));
    }

    [Theory]
    [InlineData("Secretaria", false)]
    [InlineData("Veterinario", true)]
    public async Task Handler_UsaLaAccionResueltaPorMvc_YNoLosTextosDeLaUrl(string role, bool allowed)
    {
        var user = EmployeeCookieEvents.CreatePrincipal(1, "Prueba", role);
        var route = new RouteData();
        route.Values["controller"] = "consultas";
        route.Values["action"] = "index";
        var mvc = new AuthorizationFilterContext(new ActionContext(new DefaultHttpContext(), route,
            new ControllerActionDescriptor { ControllerName = "Consultas", ActionName = "Index" }), []);
        var requirement = new RoleAccessRequirement();
        var authorization = new AuthorizationHandlerContext([requirement], user, mvc);

        await new RoleAccessHandler().HandleAsync(authorization);

        Assert.Equal(allowed, authorization.HasSucceeded);
    }

    [Fact]
    public async Task Handler_SinAccionMvc_NoAutoriza()
    {
        var requirement = new RoleAccessRequirement();
        var authorization = new AuthorizationHandlerContext([requirement],
            EmployeeCookieEvents.CreatePrincipal(1, "Prueba", RoleAccess.Administrador), new object());
        await new RoleAccessHandler().HandleAsync(authorization);
        Assert.False(authorization.HasSucceeded);
    }
}
