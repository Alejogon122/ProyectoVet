using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class EmpleadoesController : Controller
    {
        private readonly VeterinariaContext _context;

        public EmpleadoesController(VeterinariaContext context)
        {
            _context = context;
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
        public IActionResult Create()
        {
            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol");
            return View();
        }

        // POST: Empleadoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellidos,NumeroCedula,Telefono,CorreoElectronico,Contrasena,IdRol,Activo,FechaContratacion")] Empleado empleado)
        {
            if (ModelState.IsValid)
            {
                _context.Add(empleado);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol", empleado.IdRol);
            return View(empleado);
        }

        // GET: Empleadoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();
            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol", empleado.IdRol);
            return View(empleado);
        }

        // POST: Empleadoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdEmpleado,Nombre,Apellidos,NumeroCedula,Telefono,CorreoElectronico,Contrasena,IdRol,Activo,FechaContratacion")] Empleado empleado)
        {
            if (id != empleado.IdEmpleado) return NotFound();

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
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol", empleado.IdRol);
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
