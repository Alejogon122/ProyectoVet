using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class DuenoController : Controller
    {
        private readonly VeterinariaContext _context;

        public DuenoController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Dueno
        public async Task<IActionResult> Index()
        {
            return View(await _context.Duenos.ToListAsync());
        }

        // GET: Dueno/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dueno = await _context.Duenos.FirstOrDefaultAsync(m => m.IdDueno == id);
            if (dueno == null) return NotFound();

            return View(dueno);
        }

        // GET: Dueno/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Dueno/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellidos,NumeroCedula,Telefono,TelefonoOpcional,CorreoElectronico,Contrasena,Activo")] Dueno dueno)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dueno);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"¡El dueño '{dueno.Nombre} {dueno.Apellidos}' ha sido registrado exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            return View(dueno);
        }

        // GET: Dueno/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var dueno = await _context.Duenos.FindAsync(id);
            if (dueno == null) return NotFound();
            return View(dueno);
        }

        // POST: Dueno/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDueno,Nombre,Apellidos,NumeroCedula,Telefono,TelefonoOpcional,CorreoElectronico,Contrasena,Activo")] Dueno dueno)
        {
            if (id != dueno.IdDueno) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dueno);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"¡Los datos del dueño '{dueno.Nombre} {dueno.Apellidos}' han sido actualizados exitosamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DuenoExists(dueno.IdDueno)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dueno);
        }

        // GET: Dueno/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var dueno = await _context.Duenos.FirstOrDefaultAsync(m => m.IdDueno == id);
            if (dueno == null) return NotFound();

            return View(dueno);
        }

        // POST: Dueno/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dueno = await _context.Duenos.FindAsync(id);
            if (dueno != null)
            {
                _context.Duenos.Remove(dueno);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DuenoExists(int id)
        {
            return _context.Duenos.Any(e => e.IdDueno == id);
        }
    }
}
