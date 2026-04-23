using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Clinica.Web.Services;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize]
public class PacientesController : Controller
{
    private readonly ClinicaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBitacoraService _bitacora;

    public PacientesController(ClinicaDbContext context, UserManager<ApplicationUser> userManager, IBitacoraService bitacora)
    {
        _context = context;
        _userManager = userManager;
        _bitacora = bitacora;
    }

    // GET: /Pacientes
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos,Medico")]
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        const int pageSize = 20;
        ViewData["CurrentFilter"] = searchTerm;

        IQueryable<Paciente> query = _context.Pacientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(p =>
                p.Apellido.Contains(searchTerm)
                || p.Nombre.Contains(searchTerm)
                || (p.NumeroDocumento != null && p.NumeroDocumento.Contains(searchTerm))
                || (p.NumeroHistoriaClinica != null && p.NumeroHistoriaClinica.Contains(searchTerm))
                || (p.Email != null && p.Email.Contains(searchTerm)));
        }

        // Si es médico, solo ve los pacientes asociados a sus consultas o turnos
        if (User.IsInRole("Medico"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MedicoId != null)
            {
                var mid = user.MedicoId.Value;

                query = query.Where(p => 
                    _context.ConsultasMedicas.Any(c => c.MedicoId == mid && c.PacienteId == p.PacienteId) ||
                    _context.Turnos.Any(t => t.MedicoId == mid && t.PacienteId == p.PacienteId)
                );
            }
            else
            {
                // Si el usuario Medico no está vinculado a un MedicoId, no mostrar pacientes
                query = query.Where(p => false);
            }
        }

        query = query
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre);

        var pacientes = await PaginatedList<Paciente>.CreateAsync(query, pageNumber, pageSize);
        return View(pacientes);
    }

    // GET: /Pacientes/Create
    [Authorize(Roles = "Admin,Recepcionista")]
    public async Task<IActionResult> Create()
    {
        ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(), 
            "ObraSocialId", "Nombre");
        return View();
    }

    // POST: /Pacientes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Recepcionista")]
    public async Task<IActionResult> Create(Paciente paciente)
    {
        // Limpieza de datos
        paciente.Nombre = paciente.Nombre?.Trim() ?? string.Empty;
        paciente.Apellido = paciente.Apellido?.Trim() ?? string.Empty;
        paciente.NumeroDocumento = paciente.NumeroDocumento?.Trim();
        paciente.Email = paciente.Email?.Trim();

        // Control de duplicados
        if (!string.IsNullOrWhiteSpace(paciente.NumeroDocumento))
        {
            var existeDni = await _context.Pacientes.AnyAsync(p => p.NumeroDocumento == paciente.NumeroDocumento);
            if (existeDni)
            {
                ModelState.AddModelError("NumeroDocumento", "Ya existe un paciente registrado con este número de documento.");
            }
        }

        if (!string.IsNullOrWhiteSpace(paciente.Email))
        {
            var existeEmail = await _context.Pacientes.AnyAsync(p => p.Email == paciente.Email);
            if (existeEmail)
            {
                ModelState.AddModelError("Email", "Ya existe un paciente registrado con este correo electrónico.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(),
                "ObraSocialId", "Nombre", paciente.ObraSocialId);
            return View(paciente);
        }

        try
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Creó un paciente", $"PacienteId: {paciente.PacienteId} - {paciente.Apellido} {paciente.Nombre}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al intentar guardar el paciente. Por favor, intente nuevamente.");
            ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(),
                "ObraSocialId", "Nombre", paciente.ObraSocialId);
            return View(paciente);
        }
    }

    // GET: /Pacientes/Edit/5
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
    public async Task<IActionResult> Edit(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
        {
            return NotFound();
        }

        ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(), 
            "ObraSocialId", "Nombre", paciente.ObraSocialId);
        return View(paciente);
    }

    // POST: /Pacientes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
    public async Task<IActionResult> Edit(int id, Paciente paciente)
    {
        if (id != paciente.PacienteId)
        {
            return BadRequest();
        }

        // Limpieza de datos
        paciente.Nombre = paciente.Nombre?.Trim() ?? string.Empty;
        paciente.Apellido = paciente.Apellido?.Trim() ?? string.Empty;
        paciente.NumeroDocumento = paciente.NumeroDocumento?.Trim();
        paciente.Email = paciente.Email?.Trim();

        // Control de duplicados
        if (!string.IsNullOrWhiteSpace(paciente.NumeroDocumento))
        {
            var existeDni = await _context.Pacientes.AnyAsync(p => p.NumeroDocumento == paciente.NumeroDocumento && p.PacienteId != id);
            if (existeDni)
            {
                ModelState.AddModelError("NumeroDocumento", "Ya existe otro paciente registrado con este número de documento.");
            }
        }

        if (!string.IsNullOrWhiteSpace(paciente.Email))
        {
            var existeEmail = await _context.Pacientes.AnyAsync(p => p.Email == paciente.Email && p.PacienteId != id);
            if (existeEmail)
            {
                ModelState.AddModelError("Email", "Ya existe otro paciente registrado con este correo electrónico.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(),
                "ObraSocialId", "Nombre", paciente.ObraSocialId);
            return View(paciente);
        }

        try
        {
            _context.Entry(paciente).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Editó un paciente", $"PacienteId: {paciente.PacienteId} - {paciente.Apellido} {paciente.Nombre}");

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al guardar los cambios. Por favor, intente nuevamente.");
            ViewData["ObraSocialId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync(),
                "ObraSocialId", "Nombre", paciente.ObraSocialId);
            return View(paciente);
        }
    }

    // GET: /Pacientes/Delete/5
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> Delete(int id)
    {
        var paciente = await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PacienteId == id);

        if (paciente == null)
        {
            return NotFound();
        }

        return View(paciente);
    }

    // POST: /Pacientes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
        {
            return NotFound();
        }

        try
        {
            var detalle = $"PacienteId: {paciente.PacienteId} - {paciente.Apellido} {paciente.Nombre}";
            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Eliminó un paciente", detalle);
            TempData["SuccessMessage"] = "Paciente eliminado correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "No se puede eliminar el paciente porque tiene información relacionada (turnos o consultas).";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar eliminar el paciente.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Pacientes/DeleteMultiple
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> DeleteMultiple(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["ErrorMessage"] = "No se seleccionó ningún paciente para eliminar.";
            return RedirectToAction(nameof(Index));
        }

        var pacientes = await _context.Pacientes
            .Where(p => ids.Contains(p.PacienteId))
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Pacientes.RemoveRange(pacientes);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Se eliminaron {pacientes.Count} paciente(s) correctamente.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "No se pudieron eliminar los pacientes seleccionados porque al menos uno tiene dependencias (ej. turnos asociados). Ningún registro fue eliminado.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar eliminar los pacientes.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Pacientes/HistoriaClinica/5
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> HistoriaClinica(
        int id,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int? medicoId,
        int pageNumber = 1)
    {
        const int pageSize = 10;
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.PacienteId == id);

        if (paciente == null)
        {
            return NotFound();
        }

        var query = _context.ConsultasMedicas
            .Include(c => c.Medico)
            .Include(c => c.Diagnosticos)
            .Include(c => c.Recetas)
            .Where(c => c.PacienteId == id)
            .AsQueryable();

        // Si es médico, solo ve sus propias consultas sobre ese paciente
        if (User.IsInRole("Medico"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MedicoId != null)
            {
                var mid = user.MedicoId.Value;
                query = query.Where(c => c.MedicoId == mid);
            }
        }

        if (fechaDesde.HasValue)
        {
            var desde = fechaDesde.Value.Date;
            query = query.Where(c => c.FechaConsulta.Date >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date;
            query = query.Where(c => c.FechaConsulta.Date <= hasta);
        }

        if (medicoId.HasValue)
        {
            query = query.Where(c => c.MedicoId == medicoId.Value);
        }

        var totalItems = await query.CountAsync();
        var consultas = await query
            .OrderByDescending(c => c.FechaConsulta)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var medicos = await _context.Medicos
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .ToListAsync();

        var vm = new HistoriaClinicaFiltroViewModel
        {
            Paciente = paciente,
            Consultas = consultas,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            MedicoId = medicoId,
            Medicos = medicos.Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = m.MedicoId.ToString(),
                Text = $"{m.Apellido} {m.Nombre}"
            }),
            PageIndex = pageNumber,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            TotalCount = totalItems
        };

        return View(vm);
    }

    // GET: /Pacientes/HistoriaClinicaPdf/5
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> HistoriaClinicaPdf(
        int id,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int? medicoId)
    {
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.PacienteId == id);

        if (paciente == null)
        {
            return NotFound();
        }

        var query = _context.ConsultasMedicas
            .Include(c => c.Medico)
            .Include(c => c.Diagnosticos)
            .Include(c => c.Recetas)
            .Where(c => c.PacienteId == id)
            .AsQueryable();

        if (User.IsInRole("Medico"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MedicoId != null)
            {
                var mid = user.MedicoId.Value;
                query = query.Where(c => c.MedicoId == mid);
            }
        }

        if (fechaDesde.HasValue)
        {
            var desde = fechaDesde.Value.Date;
            query = query.Where(c => c.FechaConsulta.Date >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date;
            query = query.Where(c => c.FechaConsulta.Date <= hasta);
        }

        if (medicoId.HasValue)
        {
            query = query.Where(c => c.MedicoId == medicoId.Value);
        }

        var consultas = await query
            .OrderBy(c => c.FechaConsulta)
            .ToListAsync();

        var pdfBytes = Clinica.Web.Services.HistoriaClinicaPdfService.GenerarHistoriaClinicaPdf(paciente, consultas);
        var fileName = $"HistoriaClinica_{paciente.Apellido}_{paciente.Nombre}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
