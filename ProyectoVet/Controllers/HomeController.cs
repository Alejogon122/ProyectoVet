using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Models.ViewModels;
using System.Diagnostics;

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
            if (HttpContext != null && HttpContext.Session != null && string.IsNullOrWhiteSpace(HttpContext.Session.GetString("EmpleadoNombre")))
            {
                return RedirectToAction("Login", "Auth");
            }

            var hoy = DateTime.Today;
            var finHoy = hoy.AddDays(1).AddTicks(-1);

            var vm = new DashboardViewModel
            {
                TotalMascotas = await _context.Mascotas.CountAsync(m => m.Activa),
                TotalDuenos = await _context.Duenos.CountAsync(d => d.Activo),
                TotalCitasHoy = await _context.Citas.CountAsync(c => c.FechaHoraInicio >= hoy && c.FechaHoraInicio <= finHoy && c.EstadoCita != "Cancelada"),
                TotalConsultas = await _context.Consultas.CountAsync(),
                ProximasCitas = await _context.Citas
                    .Include(c => c.IdMascotaNavigation)
                        .ThenInclude(m => m.IdDuenoNavigation!)
                    .Include(c => c.IdEmpleadoNavigation!)
                    .Where(c => c.FechaHoraInicio >= hoy && c.EstadoCita != "Cancelada")
                    .OrderBy(c => c.FechaHoraInicio)
                    .Take(7)
                    .ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier ?? "unknown";
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
