using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
public class ObrasSocialesController : Controller
{
    private readonly ClinicaDbContext _context;

    public ObrasSocialesController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /ObrasSociales
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        const int pageSize = 10;
        var query = _context.ObrasSociales.AsNoTracking().OrderBy(o => o.Nombre);
        
        var list = await PaginatedList<ObraSocial>.CreateAsync(query, pageNumber, pageSize);
        return View(list);
    }

    // GET: /ObrasSociales/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /ObrasSociales/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObraSocial obraSocial)
    {
        if (ModelState.IsValid)
        {
            _context.ObrasSociales.Add(obraSocial);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(obraSocial);
    }

    // GET: /ObrasSociales/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var obraSocial = await _context.ObrasSociales.FindAsync(id);
        if (obraSocial == null)
        {
            return NotFound();
        }
        return View(obraSocial);
    }

    // POST: /ObrasSociales/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ObraSocial obraSocial)
    {
        if (id != obraSocial.ObraSocialId)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            _context.Update(obraSocial);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(obraSocial);
    }

    // GET: /ObrasSociales/Delete/5
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> Delete(int id)
    {
        var obraSocial = await _context.ObrasSociales
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ObraSocialId == id);
        
        if (obraSocial == null)
        {
            return NotFound();
        }

        return View(obraSocial);
    }

    // POST: /ObrasSociales/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var obraSocial = await _context.ObrasSociales.FindAsync(id);
        if (obraSocial == null)
        {
            return NotFound();
        }

        try 
        {
            _context.ObrasSociales.Remove(obraSocial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Obra social eliminada correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "No se puede eliminar la obra social porque tiene pacientes asociados.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    // POST: /ObrasSociales/DeleteMultiple
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> DeleteMultiple(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["ErrorMessage"] = "No se seleccionó ninguna obra social para eliminar.";
            return RedirectToAction(nameof(Index));
        }

        var obrasSociales = await _context.ObrasSociales
            .Where(o => ids.Contains(o.ObraSocialId))
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.ObrasSociales.RemoveRange(obrasSociales);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Se eliminaron {obrasSociales.Count} obra(s) social(es) correctamente.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "No se pudieron eliminar las obras sociales seleccionadas porque al menos una tiene dependencias (ej. pacientes asociados). Ningún registro fue eliminado.";
        }

        return RedirectToAction(nameof(Index));
    }
}

