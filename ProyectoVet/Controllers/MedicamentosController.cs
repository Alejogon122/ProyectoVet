using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoVet.Data;
using ProyectoVet.Models;

namespace ProyectoVet.Controllers
{
    public class MedicamentosController : Controller
    {
        private readonly VeterinariaContext _context;

        public MedicamentosController(VeterinariaContext context)
        {
            _context = context;
        }

        // GET: Medicamentos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Medicamentos.ToListAsync());
        }

        // GET: Medicamentos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);
            if (medicamento == null) return NotFound();

            return View(medicamento);
        }

        // GET: Medicamentos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Medicamentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreMedicamento,PrincipioActivo,Presentacion,Concentracion,Activo,Observaciones")] Medicamento medicamento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicamento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(medicamento);
        }

        // GET: Medicamentos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos.FindAsync(id);
            if (medicamento == null) return NotFound();
            return View(medicamento);
        }

        // POST: Medicamentos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdMedicamento,NombreMedicamento,PrincipioActivo,Presentacion,Concentracion,Activo,Observaciones")] Medicamento medicamento)
        {
            if (id != medicamento.IdMedicamento) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicamento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicamentoExists(medicamento.IdMedicamento)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(medicamento);
        }

        // GET: Medicamentos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);
            if (medicamento == null) return NotFound();

            return View(medicamento);
        }

        // POST: Medicamentos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicamento = await _context.Medicamentos.FindAsync(id);
            if (medicamento != null)
            {
                _context.Medicamentos.Remove(medicamento);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MedicamentoExists(int id)
        {
            return _context.Medicamentos.Any(e => e.IdMedicamento == id);
        }
    }
}
