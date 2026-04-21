using System.Diagnostics;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ClinicaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ClinicaDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
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

        // Si es médico, filtrar solo sus turnos
        if (User.IsInRole("Medico"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MedicoId != null)
            {
                var mid = user.MedicoId.Value;
                turnosHoyQuery = turnosHoyQuery.Where(t => t.MedicoId == mid);
            }
            else
            {
                turnosHoyQuery = turnosHoyQuery.Where(t => false);
            }
        }

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
