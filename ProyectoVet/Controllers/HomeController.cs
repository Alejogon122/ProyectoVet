using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProyectoVet.Security;

namespace ProyectoVet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly VeterinariaContext _context;

        public HomeController(ILogger<HomeController> logger, VeterinariaContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext != null && User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var hoy = DateTime.Today;
            var finHoy = hoy.AddDays(1).AddTicks(-1);
            var role = HttpContext == null ? "" : RoleAccess.CurrentRole(User);
            var citas = _context.Citas.AsNoTracking().AsQueryable();
            if (role == RoleAccess.Veterinario)
            {
                int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var empleadoId);
                citas = citas.Where(c => c.IdEmpleado == empleadoId);
            }

            var vm = new DashboardViewModel
            {
                TotalMascotas = await _context.Mascotas.CountAsync(m => m.Activa),
                TotalDuenos = await _context.Duenos.CountAsync(d => d.Activo),
                TotalCitasHoy = await citas.CountAsync(c => c.FechaHoraInicio >= hoy && c.FechaHoraInicio <= finHoy && c.EstadoCita != "Cancelada"),
                TotalConsultas = role == RoleAccess.Secretaria ? 0 : await _context.Consultas.CountAsync(),
                ProximasCitas = await citas
                    .Include(c => c.IdMascotaNavigation)
                        .ThenInclude(m => m.IdDuenoNavigation!)
                    .Include(c => c.IdEmpleadoNavigation!)
                    .Where(c => c.FechaHoraInicio >= hoy && c.EstadoCita != "Cancelada")
                    .OrderBy(c => c.FechaHoraInicio)
                    .Take(7)
                    .ToListAsync()
            };

            return role switch
            {
                RoleAccess.Veterinario => View("Veterinario", vm),
                RoleAccess.Secretaria => View("Secretaria", vm),
                _ => View(vm)
            };
        }

        public async Task<IActionResult> Perfil()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Challenge();
            var perfil = await _context.Empleados.AsNoTracking().Where(e => e.IdEmpleado == id && e.Activo)
                .Select(e => new PerfilViewModel
                {
                    Nombre = e.Nombre + " " + e.Apellidos,
                    Correo = e.CorreoElectronico,
                    Telefono = e.Telefono,
                    Rol = e.IdRolNavigation.NombreRol
                }).SingleOrDefaultAsync();
            if (perfil == null) return NotFound();
            perfil.Rol = RoleAccess.Normalize(perfil.Rol);
            return View(perfil);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier ?? "unknown";
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
