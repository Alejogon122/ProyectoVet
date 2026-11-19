using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ProyectoVet.Security;

namespace ProyectoVet.Controllers
{
    public class AuthController : Controller
    {
        private readonly VeterinariaContext _context;

        public AuthController(VeterinariaContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correoElectronico, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(contrasena))
            {
                ModelState.AddModelError(string.Empty, "Escribe correo y contraseña.");
                return View();
            }

            var empleado = await _context.Empleados
                .Include(e => e.IdRolNavigation)
                .FirstOrDefaultAsync(e => e.CorreoElectronico == correoElectronico
                    && e.Contrasena == contrasena
                    && e.Activo);

            if (empleado == null || !RoleAccess.Roles.Contains(empleado.IdRolNavigation?.NombreRol))
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o el usuario está inactivo.");
                return View();
            }

            HttpContext.Session.SetString("EmpleadoId", empleado.IdEmpleado.ToString());
            HttpContext.Session.SetString("EmpleadoNombre", empleado.Nombre + " " + empleado.Apellidos);
            HttpContext.Session.SetString("EmpleadoRol", empleado.IdRolNavigation?.NombreRol ?? "Empleado");

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                EmployeeCookieEvents.CreatePrincipal(empleado.IdEmpleado,
                    empleado.Nombre + " " + empleado.Apellidos, empleado.IdRolNavigation!.NombreRol));

            TempData["SuccessMessage"] = $"Bienvenido(a), {empleado.Nombre} {empleado.Apellidos}.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "Sesión cerrada correctamente.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}
