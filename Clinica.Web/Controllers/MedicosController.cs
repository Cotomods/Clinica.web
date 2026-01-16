using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize]
public class MedicosController : Controller
{
    private readonly ClinicaDbContext _context;

    public MedicosController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Medicos
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> Index()
    {
        var medicos = await _context.Medicos
            .Include(m => m.Especialidad)
            .AsNoTracking()
            .ToListAsync();
        return View(medicos);
    }

    // GET: /Medicos/Create
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public IActionResult Create()
    {
        return View(new MedicoCreateViewModel());
    }

    // POST: /Medicos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> Create(MedicoCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Especialidad? especialidad = null;
        if (!string.IsNullOrWhiteSpace(model.EspecialidadNombre))
        {
            especialidad = await _context.Especialidades
                .FirstOrDefaultAsync(e => e.Nombre == model.EspecialidadNombre);

            if (especialidad == null)
            {
                especialidad = new Especialidad { Nombre = model.EspecialidadNombre };
                _context.Especialidades.Add(especialidad);
            }
        }

        var medico = new Medico
        {
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Matricula = model.Matricula
        };

        if (especialidad != null)
        {
            medico.Especialidad = especialidad;
        }

        _context.Medicos.Add(medico);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Medicos/GenerarTurnos/5
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> GenerarTurnos(int id)
    {
        var medico = await _context.Medicos.FindAsync(id);
        if (medico == null)
        {
            return NotFound();
        }

        var vm = new GenerarTurnosViewModel
        {
            MedicoId = medico.MedicoId,
            MedicoNombre = $"{medico.Apellido} {medico.Nombre}",
            Fecha = DateTime.Today,
            HoraInicio = new TimeSpan(9, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0),
            DuracionMinutos = 30
        };

        return View(vm);
    }

    // POST: /Medicos/GenerarTurnos
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,RecursosHumanos")]
    public async Task<IActionResult> GenerarTurnos(GenerarTurnosViewModel model)
    {
        var medico = await _context.Medicos.FindAsync(model.MedicoId);
        if (medico == null)
        {
            return NotFound();
        }

        if (model.HoraFin <= model.HoraInicio || model.DuracionMinutos <= 0)
        {
            ModelState.AddModelError(string.Empty, "Rango horario o duración inválidos.");
        }

        if (!ModelState.IsValid)
        {
            model.MedicoNombre = $"{medico.Apellido} {medico.Nombre}";
            return View(model);
        }

        var inicio = model.Fecha.Date + model.HoraInicio;
        var fin = model.Fecha.Date + model.HoraFin;

        // Obtenemos los horarios ya existentes para ese médico en ese día
        var existentes = await _context.Turnos
            .Where(t => t.MedicoId == medico.MedicoId && t.FechaHoraInicio.Date == model.Fecha.Date)
            .Select(t => t.FechaHoraInicio)
            .ToListAsync();

        var turnos = new List<Turno>();
        for (var actual = inicio; actual < fin; actual = actual.AddMinutes(model.DuracionMinutos))
        {
            // Si ya existe un turno con este horario, lo saltamos
            if (existentes.Contains(actual))
            {
                continue;
            }

            var turno = new Turno
            {
                MedicoId = medico.MedicoId,
                PacienteId = null,
                FechaHoraInicio = actual,
                FechaHoraFin = actual.AddMinutes(model.DuracionMinutos),
                Estado = EstadoTurno.Reservado,
                MotivoConsulta = "Disponible"
            };

            turnos.Add(turno);
        }

        if (turnos.Count > 0)
        {
            _context.Turnos.AddRange(turnos);
            await _context.SaveChangesAsync();
        }

        // Redirigir al calendario de turnos luego de generar la agenda
        return RedirectToAction("Index", "Calendario");
    }
}
