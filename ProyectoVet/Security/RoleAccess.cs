using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ProyectoVet.Security;

public static class RoleAccess
{
    public const string Policy = "RoleAccess";
    public const string Administrador = "Administrador";
    public const string Veterinario = "Veterinario";
    public const string Secretaria = "Secretaria";
    public const string Recepcionista = "Recepcionista";
    public static readonly string[] Roles = [Administrador, Veterinario, Secretaria, Recepcionista];

    public static string Normalize(string? role) => role == Recepcionista ? Secretaria : role ?? "";
    public static string CurrentRole(ClaimsPrincipal user) => Normalize(user.FindFirstValue(ClaimTypes.Role));

    // Una misma matriz controla el acceso HTTP y las opciones visibles de la interfaz.
    public static bool CanAccess(ClaimsPrincipal user, string controller, string action)
    {
        if (controller == "Auth" && action is "Login" or "AccessDenied") return true;
        if (user.Identity?.IsAuthenticated != true) return false;
        var role = CurrentRole(user);
        if (role is not (Administrador or Veterinario or Secretaria)) return false;
        if (controller == "Auth") return action == "Logout";
        if (controller == "Home") return action is "Index" or "Privacy" or "Perfil";
        var read = action is "Index" or "Details";
        var crud = read || action is "Create" or "Edit" or "Delete";
        if (!crud) return false;
        return controller switch
        {
            "Empleadoes" or "Permisoes" or "Especialidades" => role == Administrador,
            "Dueno" or "Mascotas" or "Citas" => role is Administrador or Secretaria || read,
            "Consultas" or "ExpedientesClinicoes" => role == Administrador ||
                role == Veterinario && action != "Delete",
            "Medicamentos" => role == Administrador || role == Veterinario && read,
            _ => false
        };
    }
}

public sealed class RoleAccessRequirement : IAuthorizationRequirement { }

public sealed class RoleAccessHandler : AuthorizationHandler<RoleAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleAccessRequirement requirement)
    {
        if (context.Resource is AuthorizationFilterContext mvc &&
            mvc.ActionDescriptor is ControllerActionDescriptor action &&
            RoleAccess.CanAccess(context.User,
                action.ControllerName, action.ActionName))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
