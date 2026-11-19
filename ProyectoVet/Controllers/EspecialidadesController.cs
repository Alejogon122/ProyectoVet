using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class EspecialidadesController : Controller
    {
        private readonly VeterinariaContext _context;

        public EspecialidadesController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Especialidades
        public async Task<IActionResult> Index()
        {
            return View(await _context.Especialidades.ToListAsync());
        }

        // GET: Especialidades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var especialidade = await _context.Especialidades
                .FirstOrDefaultAsync(m => m.IdEspecialidad == id);
            if (especialidade == null) return NotFound();

            return View(especialidade);
        }

        // GET: Especialidades/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Especialidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreEspecialidad,Descripcion")] Especialidade especialidade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(especialidade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(especialidade);
        }

        // GET: Especialidades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var especialidade = await _context.Especialidades.FindAsync(id);
            if (especialidade == null) return NotFound();
            return View(especialidade);
        }

        // POST: Especialidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdEspecialidad,NombreEspecialidad,Descripcion")] Especialidade especialidade)
        {
            if (id != especialidade.IdEspecialidad) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(especialidade);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EspecialidadeExists(especialidade.IdEspecialidad)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(especialidade);
        }

        // GET: Especialidades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var especialidade = await _context.Especialidades
                .FirstOrDefaultAsync(m => m.IdEspecialidad == id);
            if (especialidade == null) return NotFound();

            return View(especialidade);
        }

        // POST: Especialidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var especialidade = await _context.Especialidades.FindAsync(id);
            if (especialidade != null)
            {
                _context.Especialidades.Remove(especialidade);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EspecialidadeExists(int id)
        {
            return _context.Especialidades.Any(e => e.IdEspecialidad == id);
        }
    }
}
