using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class ExpedientesClinicoesController : Controller
    {
        private readonly VeterinariaContext _context;

        public ExpedientesClinicoesController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: ExpedientesClinicoes
        public async Task<IActionResult> Index()
        {
            var expedientes = _context.ExpedientesClinicos.Include(e => e.IdMascotaNavigation);
            return View(await expedientes.ToListAsync());
        }

        // GET: ExpedientesClinicoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var expediente = await _context.ExpedientesClinicos
                .Include(e => e.IdMascotaNavigation)
                .FirstOrDefaultAsync(m => m.IdExpediente == id);
            if (expediente == null) return NotFound();

            return View(expediente);
        }

        // GET: ExpedientesClinicoes/Create
        public IActionResult Create()
        {
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota");
            return View();
        }

        // POST: ExpedientesClinicoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdMascota,FechaApertura,Alergias,Antecedentes,ObservacionesGenerales")] ExpedientesClinico expedientesClinico)
        {
            if (ModelState.IsValid)
            {
                _context.Add(expedientesClinico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", expedientesClinico.IdMascota);
            return View(expedientesClinico);
        }

        // GET: ExpedientesClinicoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var expediente = await _context.ExpedientesClinicos.FindAsync(id);
            if (expediente == null) return NotFound();
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", expediente.IdMascota);
            return View(expediente);
        }

        // POST: ExpedientesClinicoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdExpediente,IdMascota,FechaApertura,Alergias,Antecedentes,ObservacionesGenerales")] ExpedientesClinico expedientesClinico)
        {
            if (id != expedientesClinico.IdExpediente) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(expedientesClinico);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpedientesClinicoExists(expedientesClinico.IdExpediente)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdMascota"] = new SelectList(_context.Mascotas, "IdMascota", "NombreMascota", expedientesClinico.IdMascota);
            return View(expedientesClinico);
        }

        // GET: ExpedientesClinicoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var expediente = await _context.ExpedientesClinicos
                .Include(e => e.IdMascotaNavigation)
                .FirstOrDefaultAsync(m => m.IdExpediente == id);
            if (expediente == null) return NotFound();

            return View(expediente);
        }

        // POST: ExpedientesClinicoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expediente = await _context.ExpedientesClinicos.FindAsync(id);
            if (expediente != null)
            {
                _context.ExpedientesClinicos.Remove(expediente);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ExpedientesClinicoExists(int id)
        {
            return _context.ExpedientesClinicos.Any(e => e.IdExpediente == id);
        }
    }
}
