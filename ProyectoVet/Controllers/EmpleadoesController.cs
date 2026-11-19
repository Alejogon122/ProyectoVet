using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;
using ProyectoVet.Security;

namespace ProyectoVet.Controllers
{
    public class EmpleadoesController : Controller
    {
        private static readonly string[] RolesPredeterminados = RoleAccess.Roles;
        private readonly VeterinariaContext _context;

        public EmpleadoesController(VeterinariaContext context)
        {
            _context = context;
        }

        private async Task EnsureRolesExistAsync()
        {
            foreach (var nombreRol in RolesPredeterminados)
            {
                if (!await _context.Roles.AnyAsync(r => r.NombreRol == nombreRol))
                {
                    _context.Roles.Add(new Role { NombreRol = nombreRol });
                }
            }

            await _context.SaveChangesAsync();
        }

        // GET: Empleadoes
        public async Task<IActionResult> Index()
        {
            var empleados = _context.Empleados.Include(e => e.IdRolNavigation);
            return View(await empleados.ToListAsync());
        }

        // GET: Empleadoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var empleado = await _context.Empleados
                .Include(e => e.IdRolNavigation)
                .FirstOrDefaultAsync(m => m.IdEmpleado == id);
            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // GET: Empleadoes/Create
        public async Task<IActionResult> Create()
        {
            await EnsureRolesExistAsync();
            ViewData["IdRol"] = new SelectList(await _context.Roles.Where(r => RoleAccess.Roles.Contains(r.NombreRol)).OrderBy(r => r.NombreRol).ToListAsync(), "IdRol", "NombreRol");
            return View(new Empleado { Activo = true });
        }

        // POST: Empleadoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellidos,NumeroCedula,Telefono,CorreoElectronico,Contrasena,IdRol,Activo,FechaContratacion")] Empleado empleado)
        {
            await EnsureRolesExistAsync();

            if (!await _context.Roles.AnyAsync(r => r.IdRol == empleado.IdRol && RoleAccess.Roles.Contains(r.NombreRol)))
            {
                ModelState.AddModelError(nameof(Empleado.IdRol), "Debe seleccionar un rol válido.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(empleado);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdRol"] = new SelectList(await _context.Roles.Where(r => RoleAccess.Roles.Contains(r.NombreRol)).OrderBy(r => r.NombreRol).ToListAsync(), "IdRol", "NombreRol", empleado.IdRol);
            return View(empleado);
        }

        // GET: Empleadoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            await EnsureRolesExistAsync();
            ViewData["IdRol"] = new SelectList(await _context.Roles.Where(r => RoleAccess.Roles.Contains(r.NombreRol)).OrderBy(r => r.NombreRol).ToListAsync(), "IdRol", "NombreRol", empleado.IdRol);
            return View(empleado);
        }

        // POST: Empleadoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdEmpleado,Nombre,Apellidos,NumeroCedula,Telefono,CorreoElectronico,Contrasena,IdRol,Activo,FechaContratacion")] Empleado empleado)
        {
            if (id != empleado.IdEmpleado) return NotFound();

            if (string.IsNullOrWhiteSpace(empleado.Contrasena))
            {
                var original = await _context.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpleado == id);
                if (original == null) return NotFound();
                empleado.Contrasena = original.Contrasena;
                ModelState.Remove(nameof(Empleado.Contrasena));
            }

            await EnsureRolesExistAsync();

            if (!await _context.Roles.AnyAsync(r => r.IdRol == empleado.IdRol && RoleAccess.Roles.Contains(r.NombreRol)))
            {
                ModelState.AddModelError(nameof(Empleado.IdRol), "Debe seleccionar un rol válido.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpleadoExists(empleado.IdEmpleado)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdRol"] = new SelectList(await _context.Roles.Where(r => RoleAccess.Roles.Contains(r.NombreRol)).OrderBy(r => r.NombreRol).ToListAsync(), "IdRol", "NombreRol", empleado.IdRol);
            return View(empleado);
        }

        // GET: Empleadoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var empleado = await _context.Empleados
                .Include(e => e.IdRolNavigation)
                .FirstOrDefaultAsync(m => m.IdEmpleado == id);
            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // POST: Empleadoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                _context.Empleados.Remove(empleado);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.IdEmpleado == id);
        }
    }
}
