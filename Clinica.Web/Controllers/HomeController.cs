using System.Diagnostics;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ClinicaDbContext _context;

    public HomeController(ILogger<HomeController> logger, ClinicaDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;

        var totalPacientes = await _context.Pacientes.CountAsync();
        var totalMedicos = await _context.Medicos.CountAsync();

        var turnosHoyQuery = _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .AsNoTracking()
            .Where(t => t.FechaHoraInicio.Date == hoy);

        var turnosHoy = await turnosHoyQuery
            .OrderBy(t => t.FechaHoraInicio)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalPacientes = totalPacientes,
            TotalMedicos = totalMedicos,
            TurnosHoyTotal = turnosHoy.Count,
            TurnosHoyLibres = turnosHoy.Count(t => t.PacienteId == null),
            FechaHoy = hoy,
            TurnosHoy = turnosHoy
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
