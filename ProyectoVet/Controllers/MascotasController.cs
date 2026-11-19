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
            CargarDuenosDropDown();
            return View();
        }

        // POST: Mascotas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdMascota,NombreMascota,ClaseAnimal,OtraClaseAnimal,Raza,Sexo,FechaNacimiento,Color,NumeroMicrochip,IdDueno,Activa,Observaciones")] Mascota mascota)
        {
            NormalizarYValidarMascota(mascota);

            if (!HayDuenosActivos())
            {
                ModelState.AddModelError(string.Empty, "No hay dueños activos registrados. Debe registrar y activar al menos un dueño antes de crear una mascota.");
            }

            await ValidarMicrochipUnicoAsync(mascota);

            if (ModelState.IsValid)
            {
                _context.Add(mascota);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"¡La mascota '{mascota.NombreMascota}' fue registrada exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            CargarDuenosDropDown(mascota.IdDueno);
            return View(mascota);
        }

        // GET: Mascotas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mascota = await _context.Mascotas.FindAsync(id);
            if (mascota == null) return NotFound();
            CargarDuenosDropDown(mascota.IdDueno);
            return View(mascota);
        }

        // POST: Mascotas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdMascota,NombreMascota,ClaseAnimal,OtraClaseAnimal,Raza,Sexo,FechaNacimiento,Color,NumeroMicrochip,IdDueno,Activa,Observaciones")] Mascota mascota)
        {
            if (id != mascota.IdMascota) return NotFound();

            NormalizarYValidarMascota(mascota);

            if (!HayDuenosActivos())
            {
                ModelState.AddModelError(string.Empty, "No hay dueños activos registrados. Debe registrar y activar al menos un dueño antes de actualizar una mascota.");
            }

            await ValidarMicrochipUnicoAsync(mascota);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mascota);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"¡Los datos de '{mascota.NombreMascota}' fueron actualizados correctamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MascotaExists(mascota.IdMascota)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarDuenosDropDown(mascota.IdDueno);
            return View(mascota);
        }

        private async Task ValidarMicrochipUnicoAsync(Mascota mascota)
        {
            if (string.IsNullOrWhiteSpace(mascota.NumeroMicrochip)) return;

            var existeMicrochip = await _context.Mascotas.AnyAsync(m =>
                m.IdMascota != mascota.IdMascota &&
                m.NumeroMicrochip != null &&
                m.NumeroMicrochip == mascota.NumeroMicrochip);

            if (existeMicrochip)
            {
                ModelState.AddModelError(nameof(Mascota.NumeroMicrochip), "Ya existe una mascota registrada con este número de microchip.");
            }
        }

        private void NormalizarYValidarMascota(Mascota mascota)
        {
            mascota.NombreMascota = mascota.NombreMascota?.Trim()!;
            mascota.Raza = string.IsNullOrWhiteSpace(mascota.Raza) ? null : mascota.Raza.Trim();
            mascota.Color = string.IsNullOrWhiteSpace(mascota.Color) ? null : mascota.Color.Trim();
            mascota.Observaciones = string.IsNullOrWhiteSpace(mascota.Observaciones) ? null : mascota.Observaciones.Trim();
            mascota.NumeroMicrochip = string.IsNullOrWhiteSpace(mascota.NumeroMicrochip) ? null : mascota.NumeroMicrochip.Trim();

            mascota.Sexo = string.IsNullOrWhiteSpace(mascota.Sexo) ? null : mascota.Sexo.Trim();
            if (mascota.Sexo != null && mascota.Sexo != "Macho" && mascota.Sexo != "Hembra" && mascota.Sexo != "Desconocido")
            {
                mascota.Sexo = "Desconocido";
            }

            var clase = mascota.ClaseAnimal?.Trim();
            if (string.Equals(clase, "Canino", StringComparison.OrdinalIgnoreCase) || string.Equals(clase, "Perro", StringComparison.OrdinalIgnoreCase))
            {
                mascota.ClaseAnimal = "Perro";
            }
            else if (string.Equals(clase, "Felino", StringComparison.OrdinalIgnoreCase) || string.Equals(clase, "Gato", StringComparison.OrdinalIgnoreCase))
            {
                mascota.ClaseAnimal = "Gato";
            }
            else if (string.Equals(clase, "Otro", StringComparison.OrdinalIgnoreCase) || string.Equals(clase, "Otra", StringComparison.OrdinalIgnoreCase))
            {
                mascota.ClaseAnimal = "Otra";
            }
            else if (!string.IsNullOrWhiteSpace(clase))
            {
                if (string.IsNullOrWhiteSpace(mascota.OtraClaseAnimal))
                {
                    mascota.OtraClaseAnimal = clase;
                }
                mascota.ClaseAnimal = "Otra";
            }

            if (mascota.ClaseAnimal == "Perro" || mascota.ClaseAnimal == "Gato")
            {
                mascota.OtraClaseAnimal = null;
                ModelState.Remove(nameof(Mascota.OtraClaseAnimal));
            }
            else if (mascota.ClaseAnimal == "Otra")
            {
                mascota.OtraClaseAnimal = string.IsNullOrWhiteSpace(mascota.OtraClaseAnimal) ? null : mascota.OtraClaseAnimal.Trim();
                if (string.IsNullOrWhiteSpace(mascota.OtraClaseAnimal))
                {
                    ModelState.AddModelError(nameof(Mascota.OtraClaseAnimal), "Debe especificar la especie alternativa cuando selecciona 'Otra'.");
                }
            }

            if (mascota.FechaNacimiento.HasValue && mascota.FechaNacimiento.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(nameof(Mascota.FechaNacimiento), "La fecha de nacimiento no puede ser una fecha futura.");
            }

            ModelState.Remove(nameof(Mascota.IdDuenoNavigation));
        }

        private void CargarDuenosDropDown(object? selectedId = null)
        {
            var duenos = _context.Duenos
                .Where(d => d.Activo)
                .OrderBy(d => d.Nombre)
                .Select(d => new
                {
                    d.IdDueno,
                    NombreCompleto = $"{d.Nombre} {d.Apellidos} (Cédula: {d.NumeroCedula})"
                })
                .ToList();

            ViewBag.IdDueno = new SelectList(duenos, "IdDueno", "NombreCompleto", selectedId);
            var noHayDuenosActivos = duenos.Count == 0;
            ViewBag.NoHayDuenosActivos = noHayDuenosActivos;
            if (noHayDuenosActivos)
            {
                ViewBag.MensajeDuenosActivos = "No hay dueños activos registrados. Debe registrar y activar un dueño antes de crear una mascota.";
            }
        }

        private bool HayDuenosActivos()
        {
            return _context.Duenos.Any(d => d.Activo);
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
