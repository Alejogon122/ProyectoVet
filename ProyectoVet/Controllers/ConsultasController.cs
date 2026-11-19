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
            var consultas = ConsultarConsultasConRelaciones()
                .OrderByDescending(c => c.FechaConsulta);
            return View(await consultas.ToListAsync());
        }

        // GET: Consultas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var consulta = await ConsultarConsultasConRelaciones()
                .FirstOrDefaultAsync(m => m.IdConsulta == id);
            if (consulta == null) return NotFound();

            return View(consulta);
        }

        // GET: Consultas/Create
        public IActionResult Create()
        {
            CargarConsultasDropDowns();
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
                TempData["SuccessMessage"] = "¡La consulta médica ha sido registrada exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            CargarConsultasDropDowns(consulta.IdExpediente, consulta.IdCita, consulta.IdVeterinario);
            return View(consulta);
        }

        // GET: Consultas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var consulta = await _context.Consultas.FindAsync(id);
            if (consulta == null) return NotFound();
            CargarConsultasDropDowns(consulta.IdExpediente, consulta.IdCita, consulta.IdVeterinario);
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
                    TempData["SuccessMessage"] = "¡La consulta médica ha sido actualizada exitosamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsultaExists(consulta.IdConsulta)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarConsultasDropDowns(consulta.IdExpediente, consulta.IdCita, consulta.IdVeterinario);
            return View(consulta);
        }

        private IQueryable<Consulta> ConsultarConsultasConRelaciones()
        {
            return _context.Consultas
                .Include(c => c.IdCitaNavigation)
                .Include(c => c.IdExpedienteNavigation)
                    .ThenInclude(e => e.IdMascotaNavigation)
                        .ThenInclude(m => m.IdDuenoNavigation!)
                .Include(c => c.IdVeterinarioNavigation!);
        }

        private void CargarConsultasDropDowns(object? expedienteId = null, object? citaId = null, object? veterinarioId = null)
        {
            var expedientes = _context.ExpedientesClinicos
                .Include(e => e.IdMascotaNavigation)
                    .ThenInclude(m => m.IdDuenoNavigation)
                .OrderBy(e => e.IdMascotaNavigation != null ? e.IdMascotaNavigation.NombreMascota : string.Empty)
                .Select(e => new
                {
                    e.IdExpediente,
                    Descripcion = e.IdMascotaNavigation == null
                        ? $"Exp. #{e.IdExpediente} - Mascota no disponible"
                        : $"Exp. #{e.IdExpediente} - {e.IdMascotaNavigation.NombreMascota} ({e.IdMascotaNavigation.ClaseAnimal} - Dueño: {(e.IdMascotaNavigation.IdDuenoNavigation != null ? $"{e.IdMascotaNavigation.IdDuenoNavigation.Nombre} {e.IdMascotaNavigation.IdDuenoNavigation.Apellidos}" : "Sin dueño")})"
                })
                .ToList();

            var citas = _context.Citas
                .Include(c => c.IdMascotaNavigation)
                .Where(c => c.EstadoCita != "Cancelada")
                .OrderByDescending(c => c.FechaHoraInicio)
                .Select(c => new
                {
                    c.IdCita,
                    Descripcion = c.IdMascotaNavigation == null
                        ? $"Cita #{c.IdCita} - Mascota no disponible ({c.FechaHoraInicio.ToString("dd/MM/yyyy hh:mm tt")} - {c.Motivo ?? "Sin motivo"})"
                        : $"Cita #{c.IdCita} - {c.IdMascotaNavigation.NombreMascota} ({c.FechaHoraInicio.ToString("dd/MM/yyyy hh:mm tt")} - {c.Motivo ?? "Sin motivo"})"
                })
                .ToList();

            var veterinarios = _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new
                {
                    e.IdEmpleado,
                    NombreCompleto = $"Dr(a). {e.Nombre} {e.Apellidos}"
                })
                .ToList();

            ViewBag.IdExpediente = new SelectList(expedientes, "IdExpediente", "Descripcion", expedienteId);
            ViewBag.IdCita = new SelectList(citas, "IdCita", "Descripcion", citaId);
            ViewBag.IdVeterinario = new SelectList(veterinarios, "IdEmpleado", "NombreCompleto", veterinarioId);
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
