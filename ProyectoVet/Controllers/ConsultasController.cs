using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class ConsultasController : Controller
    {
        private readonly VeterinariaContext _context;

        public ConsultasController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Consultas
        public async Task<IActionResult> Index()
        {
            var consultas = _context.Consultas
                .Include(c => c.IdCitaNavigation)
                .Include(c => c.IdExpedienteNavigation)
                .Include(c => c.IdVeterinarioNavigation);
            return View(await consultas.ToListAsync());
        }

        // GET: Consultas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var consulta = await _context.Consultas
                .Include(c => c.IdCitaNavigation)
                .Include(c => c.IdExpedienteNavigation)
                .Include(c => c.IdVeterinarioNavigation)
                .FirstOrDefaultAsync(m => m.IdConsulta == id);
            if (consulta == null) return NotFound();

            return View(consulta);
        }

        // GET: Consultas/Create
        public IActionResult Create()
        {
            ViewData["IdCita"] = new SelectList(_context.Citas, "IdCita", "IdCita");
            ViewData["IdExpediente"] = new SelectList(_context.ExpedientesClinicos, "IdExpediente", "IdExpediente");
            ViewData["IdVeterinario"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre");
            return View();
        }

        // POST: Consultas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdConsulta,IdExpediente,IdCita,IdVeterinario,FechaConsulta,MotivoConsulta,Sintomas,Diagnostico,TratamientoIndicado,PesoKg,TemperaturaC,Observaciones")] Consulta consulta)
        {
            if (ModelState.IsValid)
            {
                _context.Add(consulta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCita"] = new SelectList(_context.Citas, "IdCita", "IdCita", consulta.IdCita);
            ViewData["IdExpediente"] = new SelectList(_context.ExpedientesClinicos, "IdExpediente", "IdExpediente", consulta.IdExpediente);
            ViewData["IdVeterinario"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", consulta.IdVeterinario);
            return View(consulta);
        }

        // GET: Consultas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var consulta = await _context.Consultas.FindAsync(id);
            if (consulta == null) return NotFound();
            ViewData["IdCita"] = new SelectList(_context.Citas, "IdCita", "IdCita", consulta.IdCita);
            ViewData["IdExpediente"] = new SelectList(_context.ExpedientesClinicos, "IdExpediente", "IdExpediente", consulta.IdExpediente);
            ViewData["IdVeterinario"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", consulta.IdVeterinario);
            return View(consulta);
        }

        // POST: Consultas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdConsulta,IdExpediente,IdCita,IdVeterinario,FechaConsulta,MotivoConsulta,Sintomas,Diagnostico,TratamientoIndicado,PesoKg,TemperaturaC,Observaciones")] Consulta consulta)
        {
            if (id != consulta.IdConsulta) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(consulta);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsultaExists(consulta.IdConsulta)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCita"] = new SelectList(_context.Citas, "IdCita", "IdCita", consulta.IdCita);
            ViewData["IdExpediente"] = new SelectList(_context.ExpedientesClinicos, "IdExpediente", "IdExpediente", consulta.IdExpediente);
            ViewData["IdVeterinario"] = new SelectList(_context.Empleados, "IdEmpleado", "Nombre", consulta.IdVeterinario);
            return View(consulta);
        }

        // GET: Consultas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var consulta = await _context.Consultas
                .Include(c => c.IdCitaNavigation)
                .Include(c => c.IdExpedienteNavigation)
                .Include(c => c.IdVeterinarioNavigation)
                .FirstOrDefaultAsync(m => m.IdConsulta == id);
            if (consulta == null) return NotFound();

            return View(consulta);
        }

        // POST: Consultas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consulta = await _context.Consultas.FindAsync(id);
            if (consulta != null)
            {
                _context.Consultas.Remove(consulta);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ConsultaExists(int id)
        {
            return _context.Consultas.Any(e => e.IdConsulta == id);
        }
    }
}
