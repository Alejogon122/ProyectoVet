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
            var citas = ConsultarCitasConRelaciones()
                .OrderByDescending(c => c.FechaHoraInicio);
            return View(await citas.ToListAsync());
        }

        // GET: Citas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cita = await ConsultarCitasConRelaciones()
                .FirstOrDefaultAsync(m => m.IdCita == id);
            if (cita == null) return NotFound();

            return View(cita);
        }

        // GET: Citas/Create
        public IActionResult Create()
        {
            CargarCitasDropDowns();

            var fechaSugerida = DateTime.Today.AddDays(1).AddHours(9);
            if (fechaSugerida.DayOfWeek == DayOfWeek.Sunday)
            {
                fechaSugerida = fechaSugerida.AddDays(1);
            }

            var cita = new Cita
            {
                FechaHoraInicio = fechaSugerida,
                FechaHoraFin = fechaSugerida.AddMinutes(30),
                EstadoCita = "Pendiente"
            };

            return View(cita);
        }

        // POST: Citas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCita,IdMascota,IdEmpleado,FechaHoraInicio,FechaHoraFin,EstadoCita,Motivo,Observaciones,FechaCreacion,CreadaPorEmpleadoId")] Cita cita)
        {
            await NormalizarYValidarCitaAsync(cita);

            if (ModelState.IsValid)
            {
                _context.Add(cita);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡La cita ha sido programada exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            CargarCitasDropDowns(cita.IdMascota, cita.IdEmpleado, cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        // GET: Citas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();
            CargarCitasDropDowns(cita.IdMascota, cita.IdEmpleado, cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        // POST: Citas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCita,IdMascota,IdEmpleado,FechaHoraInicio,FechaHoraFin,EstadoCita,Motivo,Observaciones,FechaCreacion,CreadaPorEmpleadoId")] Cita cita)
        {
            if (id != cita.IdCita) return NotFound();

            await NormalizarYValidarCitaAsync(cita);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cita);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "¡La cita ha sido actualizada exitosamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CitaExists(cita.IdCita)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            CargarCitasDropDowns(cita.IdMascota, cita.IdEmpleado, cita.CreadaPorEmpleadoId);
            return View(cita);
        }

        private IQueryable<Cita> ConsultarCitasConRelaciones()
        {
            return _context.Citas
                .Include(c => c.IdMascotaNavigation!)
                    .ThenInclude(m => m.IdDuenoNavigation)
                .Include(c => c.IdEmpleadoNavigation)
                .Include(c => c.CreadaPorEmpleado);
        }

        private async Task NormalizarYValidarCitaAsync(Cita cita)
        {
            cita.Motivo = string.IsNullOrWhiteSpace(cita.Motivo) ? "Consulta general" : cita.Motivo.Trim();
            cita.Observaciones = string.IsNullOrWhiteSpace(cita.Observaciones) ? null : cita.Observaciones.Trim();

            var estado = cita.EstadoCita?.Trim();
            if (string.Equals(estado, "En Atención", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estado, "En Atencion", StringComparison.OrdinalIgnoreCase))
            {
                cita.EstadoCita = "Confirmada";
            }
            else if (string.Equals(estado, "No asistió", StringComparison.OrdinalIgnoreCase))
            {
                cita.EstadoCita = "No asistio";
            }
            else if (!string.IsNullOrWhiteSpace(estado))
            {
                cita.EstadoCita = estado;
            }
            else
            {
                cita.EstadoCita = "Pendiente";
            }

            var estadosValidos = new[] { "Pendiente", "Confirmada", "Completada", "Cancelada", "No asistio" };
            if (!estadosValidos.Contains(cita.EstadoCita))
            {
                ModelState.AddModelError(nameof(Cita.EstadoCita), "El estado de la cita debe ser Pendiente, Confirmada, Completada, Cancelada o No asistio.");
            }

            if (cita.FechaCreacion == default)
            {
                cita.FechaCreacion = DateTime.Now;
            }

            // Validaciones según restricciones de base de datos
            // 1. Rango horario: Fin > Inicio
            if (cita.FechaHoraFin <= cita.FechaHoraInicio)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraFin), "La hora de finalización debe ser posterior a la hora de inicio.");
            }

            // 2. Mismo día
            if (cita.FechaHoraInicio.Date != cita.FechaHoraFin.Date)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraFin), "La cita debe iniciar y finalizar el mismo día.");
            }

            // 3. Duración: entre 5 y 480 minutos
            var duracionMinutos = (cita.FechaHoraFin - cita.FechaHoraInicio).TotalMinutes;
            if (duracionMinutos < 5 || duracionMinutos > 480)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraFin), "La duración de la cita debe estar entre 5 minutos y 8 horas (480 minutos).");
            }

            // 4. Día de atención: Lunes a Sábado
            if (cita.FechaHoraInicio.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraInicio), "La clínica no atiende los domingos. Debe agendar entre lunes y sábado.");
            }

            // 5. Horario de atención: 08:00 a 19:00
            var horaInicio = cita.FechaHoraInicio.TimeOfDay;
            var horaFin = cita.FechaHoraFin.TimeOfDay;
            var horaApertura = new TimeSpan(8, 0, 0);
            var horaCierre = new TimeSpan(19, 0, 0);

            if (horaInicio < horaApertura || horaInicio >= horaCierre)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraInicio), "Las citas deben iniciar entre las 08:00 y las 18:59.");
            }

            if (horaFin > horaCierre)
            {
                ModelState.AddModelError(nameof(Cita.FechaHoraFin), "La cita no puede finalizar después de las 19:00.");
            }

            // 6. Conflicto de horario del Veterinario
            if (cita.IdEmpleado > 0 && cita.EstadoCita != "Cancelada")
            {
                var solapamientoVeterinario = await _context.Citas.AnyAsync(c =>
                    c.IdCita != cita.IdCita &&
                    c.IdEmpleado == cita.IdEmpleado &&
                    c.EstadoCita != "Cancelada" &&
                    cita.FechaHoraInicio < c.FechaHoraFin &&
                    cita.FechaHoraFin > c.FechaHoraInicio);

                if (solapamientoVeterinario)
                {
                    ModelState.AddModelError(nameof(Cita.IdEmpleado), "El veterinario seleccionado ya tiene otra cita activa programada en ese horario.");
                }
            }

            // 7. Conflicto de horario de la Mascota
            if (cita.IdMascota > 0 && cita.EstadoCita != "Cancelada")
            {
                var solapamientoMascota = await _context.Citas.AnyAsync(c =>
                    c.IdCita != cita.IdCita &&
                    c.IdMascota == cita.IdMascota &&
                    c.EstadoCita != "Cancelada" &&
                    cita.FechaHoraInicio < c.FechaHoraFin &&
                    cita.FechaHoraFin > c.FechaHoraInicio);

                if (solapamientoMascota)
                {
                    ModelState.AddModelError(nameof(Cita.IdMascota), "La mascota seleccionada ya tiene otra cita activa programada en ese horario.");
                }
            }

            // Remover referencias de navegación para evitar validaciones espurias
            ModelState.Remove(nameof(Cita.IdEmpleadoNavigation));
            ModelState.Remove(nameof(Cita.IdMascotaNavigation));
            ModelState.Remove(nameof(Cita.CreadaPorEmpleado));
            ModelState.Remove(nameof(Cita.Consulta));
        }

        private void CargarCitasDropDowns(object? mascotaId = null, object? veterinarioId = null, object? creadorId = null)
        {
            var mascotas = _context.Mascotas
                .Include(m => m.IdDuenoNavigation)
                .Where(m => m.Activa)
                .OrderBy(m => m.NombreMascota)
                .Select(m => new
                {
                    m.IdMascota,
                    Descripcion = m.NombreMascota + " - " + m.ClaseAnimal + " (Dueño: " + (m.IdDuenoNavigation != null ? m.IdDuenoNavigation.Nombre + " " + m.IdDuenoNavigation.Apellidos : "Sin dueño") + ")"
                })
                .ToList();

            var empleados = _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new
                {
                    e.IdEmpleado,
                    NombreCompleto = e.Nombre + " " + e.Apellidos,
                    Rol = e.IdRolNavigation.NombreRol
                })
                .ToList();

            var veterinarios = empleados
                .Where(e => e.Rol == "Veterinario")
                .Select(e => new
                {
                    e.IdEmpleado,
                    NombreCompleto = "Dr(a). " + e.NombreCompleto
                })
                .ToList();

            ViewBag.IdMascota = new SelectList(mascotas, "IdMascota", "Descripcion", mascotaId);
            ViewBag.IdEmpleado = new SelectList(veterinarios, "IdEmpleado", "NombreCompleto", veterinarioId);
            ViewBag.CreadaPorEmpleadoId = new SelectList(empleados, "IdEmpleado", "NombreCompleto", creadorId);

            ViewBag.NoHayMascotasActivas = mascotas.Count == 0;
            ViewBag.NoHayVeterinariosActivos = veterinarios.Count == 0;
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
