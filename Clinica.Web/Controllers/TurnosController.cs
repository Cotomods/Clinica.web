using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize]
public class TurnosController : Controller
{
    private readonly ClinicaDbContext _context;

    public TurnosController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Turnos
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
    public async Task<IActionResult> Index(DateTime? fechaDesde, DateTime? fechaHasta, int? medicoId, int? pacienteId)
    {
        var query = _context.Turnos
            .Include(t => t.Medico)
            .Include(t => t.Paciente)
            .AsNoTracking()
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = fechaDesde.Value.Date;
            query = query.Where(t => t.FechaHoraInicio.Date >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date;
            query = query.Where(t => t.FechaHoraInicio.Date <= hasta);
        }

        if (medicoId.HasValue)
        {
            query = query.Where(t => t.MedicoId == medicoId.Value);
        }

        if (pacienteId.HasValue)
        {
            query = query.Where(t => t.PacienteId == pacienteId.Value);
        }

        var turnos = await query
            .OrderBy(t => t.FechaHoraInicio)
            .ToListAsync();

        ViewBag.MedicoId = new SelectList(await _context.Medicos.AsNoTracking().ToListAsync(), "MedicoId", "Apellido", medicoId);
        ViewBag.PacienteId = new SelectList(await _context.Pacientes.AsNoTracking().ToListAsync(), "PacienteId", "Apellido", pacienteId);

        ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
        ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");

        return View(turnos);
    }

    // GET: /Turnos/Edit/5
    [Authorize(Roles = "Admin,Recepcionista")]
    public async Task<IActionResult> Edit(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null)
        {
            return NotFound();
        }

        await LoadCombosAsync(turno.MedicoId, turno.PacienteId);
        return View(turno);
    }

    // POST: /Turnos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Recepcionista")]
    public async Task<IActionResult> Edit(int id, Turno turno)
    {
        if (id != turno.TurnoId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadCombosAsync(turno.MedicoId, turno.PacienteId);
            return View(turno);
        }

        _context.Entry(turno).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Turnos/Delete/5
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> Delete(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Medico)
            .Include(t => t.Paciente)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TurnoId == id);

        if (turno == null)
        {
            return NotFound();
        }

        return View(turno);
    }

    // POST: /Turnos/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null)
        {
            return NotFound();
        }

        _context.Turnos.Remove(turno);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCombosAsync(int medicoId, int? pacienteId)
    {
        ViewBag.MedicoId = new SelectList(await _context.Medicos.AsNoTracking().ToListAsync(), "MedicoId", "Apellido", medicoId);
        ViewBag.PacienteId = new SelectList(await _context.Pacientes.AsNoTracking().ToListAsync(), "PacienteId", "Apellido", pacienteId);
    }
}
