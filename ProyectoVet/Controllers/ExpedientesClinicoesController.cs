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
            var expedientes = _context.ExpedientesClinicos
                .Include(e => e.IdMascotaNavigation!)
                    .ThenInclude(m => m.IdDuenoNavigation)
                .OrderByDescending(e => e.FechaApertura);
            return View(await expedientes.ToListAsync());
        }

        // GET: ExpedientesClinicoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var expediente = await _context.ExpedientesClinicos
                .Include(e => e.IdMascotaNavigation!)
                    .ThenInclude(m => m.IdDuenoNavigation)
                .Include(e => e.Consulta)
                    .ThenInclude(c => c.IdVeterinarioNavigation)
                .FirstOrDefaultAsync(m => m.IdExpediente == id);
            if (expediente == null) return NotFound();

            return View(expediente);
        }

        // GET: ExpedientesClinicoes/Create
        public IActionResult Create()
        {
            CargarMascotasDropDown();
            var expediente = new ExpedientesClinico
            {
                FechaApertura = DateTime.Now
            };
            return View(expediente);
        }

        // POST: ExpedientesClinicoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdMascota,FechaApertura,Alergias,Antecedentes,ObservacionesGenerales")] ExpedientesClinico expedientesClinico)
        {
            await NormalizarYValidarExpedienteAsync(expedientesClinico);

            if (ModelState.IsValid)
            {
                _context.Add(expedientesClinico);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡El expediente clínico ha sido creado exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            CargarMascotasDropDown(expedientesClinico.IdMascota);
            return View(expedientesClinico);
        }

        // GET: ExpedientesClinicoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var expediente = await _context.ExpedientesClinicos.FindAsync(id);
            if (expediente == null) return NotFound();

            CargarMascotasDropDown(expediente.IdMascota, expediente.IdMascota);
            return View(expediente);
        }

        // POST: ExpedientesClinicoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdExpediente,IdMascota,FechaApertura,Alergias,Antecedentes,ObservacionesGenerales")] ExpedientesClinico expedientesClinico)
        {
            if (id != expedientesClinico.IdExpediente) return NotFound();

            await NormalizarYValidarExpedienteAsync(expedientesClinico);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(expedientesClinico);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "¡El expediente clínico ha sido actualizado exitosamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpedientesClinicoExists(expedientesClinico.IdExpediente)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            CargarMascotasDropDown(expedientesClinico.IdMascota, expedientesClinico.IdMascota);
            return View(expedientesClinico);
        }

        private async Task NormalizarYValidarExpedienteAsync(ExpedientesClinico expediente)
        {
            expediente.Alergias = string.IsNullOrWhiteSpace(expediente.Alergias) ? null : expediente.Alergias.Trim();
            expediente.Antecedentes = string.IsNullOrWhiteSpace(expediente.Antecedentes) ? null : expediente.Antecedentes.Trim();
            expediente.ObservacionesGenerales = string.IsNullOrWhiteSpace(expediente.ObservacionesGenerales) ? null : expediente.ObservacionesGenerales.Trim();

            if (expediente.FechaApertura == default)
            {
                expediente.FechaApertura = DateTime.Now;
            }

            if (expediente.FechaApertura > DateTime.Now.AddDays(1))
            {
                ModelState.AddModelError(nameof(ExpedientesClinico.FechaApertura), "La fecha de apertura no puede ser una fecha futura.");
            }

            if (expediente.IdMascota <= 0)
            {
                ModelState.AddModelError(nameof(ExpedientesClinico.IdMascota), "Debe seleccionar una mascota válida.");
            }
            else
            {
                var mascotaExiste = await _context.Mascotas.AnyAsync(m => m.IdMascota == expediente.IdMascota);
                if (!mascotaExiste)
                {
                    ModelState.AddModelError(nameof(ExpedientesClinico.IdMascota), "La mascota seleccionada no existe en el sistema.");
                }
                else
                {
                    // Validar restricción UNIQUE (UQ_Expedientes_Mascota):
                    // Una mascota solo puede tener UN expediente clínico
                    var yaTieneExpediente = await _context.ExpedientesClinicos.AnyAsync(e =>
                        e.IdExpediente != expediente.IdExpediente &&
                        e.IdMascota == expediente.IdMascota);

                    if (yaTieneExpediente)
                    {
                        ModelState.AddModelError(nameof(ExpedientesClinico.IdMascota), "La mascota seleccionada ya cuenta con un expediente clínico registrado.");
                    }
                }
            }

            // Remover referencias de navegación para evitar falsos positivos
            ModelState.Remove(nameof(ExpedientesClinico.IdMascotaNavigation));
            ModelState.Remove(nameof(ExpedientesClinico.Consulta));
        }

        private void CargarMascotasDropDown(object? selectedId = null, int? mascotaActualId = null)
        {
            var query = _context.Mascotas
                .Include(m => m.IdDuenoNavigation)
                .Where(m => m.Activa);

            if (mascotaActualId.HasValue)
            {
                query = query.Where(m => m.IdMascota == mascotaActualId.Value ||
                                         !_context.ExpedientesClinicos.Any(e => e.IdMascota == m.IdMascota));
            }
            else
            {
                query = query.Where(m => !_context.ExpedientesClinicos.Any(e => e.IdMascota == m.IdMascota));
            }

            var mascotas = query
                .OrderBy(m => m.NombreMascota)
                .Select(m => new
                {
                    m.IdMascota,
                    Descripcion = m.NombreMascota + " - " + m.ClaseAnimal + " (Dueño: " + (m.IdDuenoNavigation != null ? m.IdDuenoNavigation.Nombre + " " + m.IdDuenoNavigation.Apellidos : "Sin dueño") + ")"
                })
                .ToList();

            ViewBag.IdMascota = new SelectList(mascotas, "IdMascota", "Descripcion", selectedId);
            ViewBag.NoHayMascotasDisponibles = mascotas.Count == 0;
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
