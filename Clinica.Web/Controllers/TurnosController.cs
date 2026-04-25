using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Clinica.Web.Services;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize]
public class TurnosController : Controller
{
    private readonly ClinicaDbContext _context;
    private readonly IBitacoraService _bitacora;

    public TurnosController(ClinicaDbContext context, IBitacoraService bitacora)
    {
        _context = context;
        _bitacora = bitacora;
    }

    // GET: /Turnos
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
    public async Task<IActionResult> Index(DateTime? fechaDesde, DateTime? fechaHasta, int? medicoId, int? pacienteId, int pageNumber = 1)
    {
        const int pageSize = 20;

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

        query = query.OrderBy(t => t.FechaHoraInicio);

        var turnos = await PaginatedList<Turno>.CreateAsync(query, pageNumber, pageSize);

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

        // Limpieza de datos
        turno.MotivoConsulta = turno.MotivoConsulta?.Trim();

        // No permitir establecer/crear horarios de turno en el pasado.
        // Permitimos editar otros campos de un turno ya pasado, siempre que no se cambie su horario.
        var existing = await _context.Turnos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TurnoId == id);

        if (existing == null)
        {
            return NotFound();
        }

        var cambioHorario = existing.FechaHoraInicio != turno.FechaHoraInicio
                           || existing.FechaHoraFin != turno.FechaHoraFin;

        if (cambioHorario)
        {
            var now = DateTime.Now;

            if (turno.FechaHoraInicio < now)
            {
                ModelState.AddModelError(nameof(Turno.FechaHoraInicio), "No se puede establecer un turno en el pasado.");
            }

            if (turno.FechaHoraFin < now)
            {
                ModelState.AddModelError(nameof(Turno.FechaHoraFin), "No se puede establecer un turno en el pasado.");
            }
        }

        ModelState.Remove(nameof(Turno.Medico));
        ModelState.Remove(nameof(Turno.Paciente));
        ModelState.Remove(nameof(Turno.Consultorio));
        ModelState.Remove(nameof(Turno.ConsultaMedica));

        if (!ModelState.IsValid)
        {
            await LoadCombosAsync(turno.MedicoId, turno.PacienteId);
            return View(turno);
        }

        try
        {
            turno.ConsultorioId = existing.ConsultorioId;
            turno.ConsultaMedicaId = existing.ConsultaMedicaId;

            _context.Entry(turno).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Editó un turno", $"TurnoId: {turno.TurnoId}");

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al guardar los cambios del turno en la base de datos.");
            await LoadCombosAsync(turno.MedicoId, turno.PacienteId);
            return View(turno);
        }
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

        try
        {
            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Eliminó un turno", $"TurnoId: {id}");
            TempData["SuccessMessage"] = "Turno eliminado correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "No se puede eliminar este turno debido a registros asociados en la base de datos.";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar eliminar el turno.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCombosAsync(int medicoId, int? pacienteId)
    {
        ViewBag.MedicoId = new SelectList(await _context.Medicos.AsNoTracking().ToListAsync(), "MedicoId", "Apellido", medicoId);
        ViewBag.PacienteId = new SelectList(await _context.Pacientes.AsNoTracking().ToListAsync(), "PacienteId", "Apellido", pacienteId);
    }
}
