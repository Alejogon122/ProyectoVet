using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;

namespace ProyectoVet.Security;

public sealed class EmployeeCookieEvents(VeterinariaContext database) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            await RejectAsync(context);
            return;
        }

        var employee = await database.Empleados.AsNoTracking()
            .Where(e => e.IdEmpleado == id && e.Activo)
            .Select(e => new { Name = e.Nombre + " " + e.Apellidos, Role = e.IdRolNavigation.NombreRol })
            .SingleOrDefaultAsync();
        if (employee == null || !RoleAccess.Roles.Contains(employee.Role))
        {
            await RejectAsync(context);
            return;
        }

        var role = RoleAccess.Normalize(employee.Role);
        if (context.Principal!.Identity!.Name != employee.Name || RoleAccess.CurrentRole(context.Principal) != role)
        {
            context.ReplacePrincipal(CreatePrincipal(id, employee.Name, role));
            context.ShouldRenew = true;
        }
    }

    public static ClaimsPrincipal CreatePrincipal(int id, string name, string role) => new(
        new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, RoleAccess.Normalize(role))
        ], CookieAuthenticationDefaults.AuthenticationScheme));

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        context.HttpContext.Session.Clear();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
