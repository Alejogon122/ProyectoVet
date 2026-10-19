using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class CitasController : Controller
    {
        private readonly VeterinariaContext _context;

        public CitasController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Citas
        public async Task<IActionResult> Index()
        {
            var citas = _context.Citas
                .Include(c => c.IdMascotaNavigation)
                .Include(c => c.IdEmpleadoNavigation)
                .Include(c => c.CreadaPorEmpleado);
            return View(await citas.ToListAsync());
        }

        // GET: Citas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cita = await _context.Citas
                .Include(c => c.IdMascotaNavigation)
                .Include(c => c.IdEmpleadoNavigation)
                .Include(c => c.CreadaPorEmpleado)
                .FirstOrDefaultAsync(m => m.IdCita == id);
            if (cita == null) return NotFound();

            return View(cita);
        }

        // GET: Citas/Create
        public IActionResult Create()
        {
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota");
            ViewData["IdEmpleado"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre");
            ViewData["CreadaPorEmpleadoId"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre");
            return View();
        }

        // POST: Citas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCita,IdMascota,IdEmpleado,FechaHoraInicio,FechaHoraFin,EstadoCita,Motivo,Observaciones,FechaCreacion,CreadaPorEmpleadoId")] Cita cita)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cita);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", cita.IdMascota);
            ViewData["IdEmpleado"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.IdEmpleado);
            ViewData["CreadaPorEmpleadoId"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        // GET: Citas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", cita.IdMascota);
            ViewData["IdEmpleado"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.IdEmpleado);
            ViewData["CreadaPorEmpleadoId"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        // POST: Citas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCita,IdMascota,IdEmpleado,FechaHoraInicio,FechaHoraFin,EstadoCita,Motivo,Observaciones,FechaCreacion,CreadaPorEmpleadoId")] Cita cita)
        {
            if (id != cita.IdCita) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cita);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CitaExists(cita.IdCita)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", cita.IdMascota);
            ViewData["IdEmpleado"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.IdEmpleado);
            ViewData["CreadaPorEmpleadoId"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        // GET: Citas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cita = await _context.Citas
                .Include(c => c.IdMascotaNavigation)
                .Include(c => c.IdEmpleadoNavigation)
                .Include(c => c.CreadaPorEmpleado)
                .FirstOrDefaultAsync(m => m.IdCita == id);
            if (cita == null) return NotFound();

            return View(cita);
        }

        // POST: Citas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita != null)
            {
                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CitaExists(int id)
        {
            return _context.Citas.Any(e => e.IdCita == id);
        }
    }
}
