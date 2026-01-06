using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

public class PacientesController : Controller
{
    private readonly ClinicaDbContext _context;

    public PacientesController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Pacientes
    public async Task<IActionResult> Index()
    {
        var pacientes = await _context.Pacientes
            .AsNoTracking()
            .ToListAsync();

        return View(pacientes);
    }

    // GET: /Pacientes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Pacientes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> HistoriaClinica(int id)
    {
        var paciente = await _context.Pacientes
            .Include(p => p.Consultas)
                .ThenInclude(c => c.Medico)
            .FirstOrDefaultAsync(p => p.PacienteId == id);

        if (paciente == null)
        {
            return NotFound();
        }

        var consultas = paciente.Consultas
            .OrderByDescending(c => c.FechaConsulta)
            .ToList();

        var vm = new HistoriaClinicaViewModel
        {
            Paciente = paciente,
            Consultas = consultas
        };

        return View(vm);
    }

    // GET: /Pacientes/HistoriaClinicaPdf/5
    public async Task<IActionResult> HistoriaClinicaPdf(int id)
    {
        var paciente = await _context.Pacientes
            .Include(p => p.Consultas)
                .ThenInclude(c => c.Medico)
            .FirstOrDefaultAsync(p => p.PacienteId == id);

        if (paciente == null)
        {
            return NotFound();
        }

        var consultas = paciente.Consultas
            .OrderBy(c => c.FechaConsulta)
            .ToList();

        var pdfBytes = Clinica.Web.Services.HistoriaClinicaPdfService.GenerarHistoriaClinicaPdf(paciente, consultas);
        var fileName = $"HistoriaClinica_{paciente.Apellido}_{paciente.Nombre}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
