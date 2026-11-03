using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;

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
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("EmpleadoNombre") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
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

            if (empleado == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o el usuario está inactivo.");
                return View();
            }

            HttpContext.Session.SetString("EmpleadoId", empleado.IdEmpleado.ToString());
            HttpContext.Session.SetString("EmpleadoNombre", empleado.Nombre + " " + empleado.Apellidos);
            HttpContext.Session.SetString("EmpleadoRol", empleado.IdRolNavigation?.NombreRol ?? "Empleado");

            TempData["SuccessMessage"] = $"Bienvenido(a), {empleado.Nombre} {empleado.Apellidos}.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "Sesión cerrada correctamente.";
            return RedirectToAction(nameof(Login));
        }
    }
}
