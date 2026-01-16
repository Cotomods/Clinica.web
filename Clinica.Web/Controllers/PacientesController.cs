using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
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

    public PacientesController(ClinicaDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Pacientes
    [Authorize(Roles = "Admin,Recepcionista,RecursosHumanos")]
    public async Task<IActionResult> Index()
    {
        var pacientes = await _context.Pacientes
            .AsNoTracking()
            .ToListAsync();

        return View(pacientes);
    }

    // GET: /Pacientes/Create
    [Authorize(Roles = "Admin,Recepcionista")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Pacientes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Recepcionista")]
    public async Task<IActionResult> Create(Paciente paciente)
    {
        if (!ModelState.IsValid)
        {
            return View(paciente);
        }

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Pacientes/HistoriaClinica/5
    [Authorize(Roles = "Admin,Medico")]
    public async Task<IActionResult> HistoriaClinica(
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

        var consultas = await query
            .OrderByDescending(c => c.FechaConsulta)
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
            })
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
