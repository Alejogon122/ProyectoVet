using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class MascotasController : Controller
    {
        private readonly VeterinariaContext _context;

        public MascotasController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Mascotas
        public async Task<IActionResult> Index()
        {
            var mascotas = _context.Mascotas.Include(m => m.IdDuenoNavigation);
            return View(await mascotas.ToListAsync());
        }

        // GET: Mascotas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mascota = await _context.Mascotas
                .Include(m => m.IdDuenoNavigation)
                .FirstOrDefaultAsync(m => m.IdMascota == id);
            if (mascota == null) return NotFound();

            return View(mascota);
        }

        // GET: Mascotas/Create
        public IActionResult Create()
        {
            ViewData["IdDueno"] = new SelectList(_context.Duenos, "IdDueno", "Nombre");
            return View();
        }

        // POST: Mascotas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdMascota,NombreMascota,ClaseAnimal,OtraClaseAnimal,Raza,Sexo,FechaNacimiento,Color,NumeroMicrochip,IdDueno,Activa,Observaciones")] Mascota mascota)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mascota);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdDueno"] = new SelectList(_context.Duenos, "IdDueno", "Nombre", mascota.IdDueno);
            return View(mascota);
        }

        // GET: Mascotas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mascota = await _context.Mascotas.FindAsync(id);
            if (mascota == null) return NotFound();
            ViewData["IdDueno"] = new SelectList(_context.Duenos, "IdDueno", "Nombre", mascota.IdDueno);
            return View(mascota);
        }

        // POST: Mascotas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdMascota,NombreMascota,ClaseAnimal,OtraClaseAnimal,Raza,Sexo,FechaNacimiento,Color,NumeroMicrochip,IdDueno,Activa,Observaciones")] Mascota mascota)
        {
            if (id != mascota.IdMascota) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mascota);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MascotaExists(mascota.IdMascota)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdDueno"] = new SelectList(_context.Duenos, "IdDueno", "Nombre", mascota.IdDueno);
            return View(mascota);
        }

        // GET: Mascotas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var mascota = await _context.Mascotas
                .Include(m => m.IdDuenoNavigation)
                .FirstOrDefaultAsync(m => m.IdMascota == id);
            if (mascota == null) return NotFound();

            return View(mascota);
        }

        // POST: Mascotas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mascota = await _context.Mascotas.FindAsync(id);
            if (mascota != null)
            {
                _context.Mascotas.Remove(mascota);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MascotaExists(int id)
        {
            return _context.Mascotas.Any(e => e.IdMascota == id);
        }
    }
}
