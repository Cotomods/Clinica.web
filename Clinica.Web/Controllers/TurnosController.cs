using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

public class TurnosController : Controller
{
    private readonly ClinicaDbContext _context;

    public TurnosController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Turnos
    public async Task<IActionResult> Index()
    {
        var turnos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .AsNoTracking()
            .OrderBy(t => t.FechaHoraInicio)
            .ToListAsync();

        return View(turnos);
    }

    // GET: /Turnos/Calendario
    public async Task<IActionResult> Calendario(DateTime? fecha, int? medicoId)
    {
        var baseDate = (fecha ?? DateTime.Today).Date;
        var firstDayOfMonth = new DateTime(baseDate.Year, baseDate.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var query = _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .AsNoTracking()
            .Where(t => t.FechaHoraInicio.Date >= firstDayOfMonth && t.FechaHoraInicio.Date <= lastDayOfMonth);

        if (medicoId.HasValue)
        {
            query = query.Where(t => t.MedicoId == medicoId.Value);
        }

        var turnosMes = await query.ToListAsync();

        var diasConTurnos = turnosMes
            .Select(t => t.FechaHoraInicio.Day)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var turnosDelDia = turnosMes
            .Where(t => t.FechaHoraInicio.Date == baseDate.Date)
            .OrderBy(t => t.FechaHoraInicio)
            .ToList();

        var vm = new CalendarioTurnosViewModel
        {
            Anio = baseDate.Year,
            Mes = baseDate.Month,
            FechaSeleccionada = baseDate,
            MedicoId = medicoId,
            DiasConTurnos = diasConTurnos,
            TurnosDelDia = turnosDelDia
        };

        ViewData["MedicoId"] = new SelectList(await _context.Medicos.AsNoTracking().ToListAsync(), "MedicoId", "Apellido", medicoId);

        return View(vm);
    }

    // GET: /Turnos/Create
    public async Task<IActionResult> Create()
    {
        ViewData["PacienteId"] = new SelectList(await _context.Pacientes.AsNoTracking().ToListAsync(), "PacienteId", "Apellido");
        ViewData["MedicoId"] = new SelectList(await _context.Medicos.AsNoTracking().ToListAsync(), "MedicoId", "Apellido");
        return View();
    }

    // POST: /Turnos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Turno turno)
    {
        if (!ModelState.IsValid)
        {
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "PacienteId", "Apellido", turno.PacienteId);
            ViewData["MedicoId"] = new SelectList(_context.Medicos, "MedicoId", "Apellido", turno.MedicoId);
            return View(turno);
        }

        // Por simplicidad, calculamos FechaHoraFin como +30 minutos
        if (turno.FechaHoraFin == default)
        {
            turno.FechaHoraFin = turno.FechaHoraInicio.AddMinutes(30);
        }

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Turnos/Asignar/5
    public async Task<IActionResult> Asignar(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Medico)
            .Include(t => t.Paciente)
            .FirstOrDefaultAsync(t => t.TurnoId == id);

        if (turno == null)
        {
            return NotFound();
        }

        if (turno.PacienteId != null)
        {
            // Ya está asignado
            return RedirectToAction(nameof(Index));
        }

        var model = new AsignarTurnoViewModel
        {
            TurnoId = turno.TurnoId,
            MedicoNombre = $"{turno.Medico.Apellido} {turno.Medico.Nombre}",
            FechaHora = turno.FechaHoraInicio
        };

        ViewData["PacienteId"] = new SelectList(await _context.Pacientes.AsNoTracking().ToListAsync(), "PacienteId", "Apellido");
        return View(model);
    }

    // POST: /Turnos/Asignar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(AsignarTurnoViewModel model)
    {
        var turno = await _context.Turnos
            .Include(t => t.Medico)
            .FirstOrDefaultAsync(t => t.TurnoId == model.TurnoId);

        if (turno == null)
        {
            return NotFound();
        }

        // No permitir asignar turnos en el pasado
        if (turno.FechaHoraInicio < DateTime.Now)
        {
            ModelState.AddModelError(string.Empty, "No se puede asignar un turno en el pasado.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "PacienteId", "Apellido", model.PacienteId);
            model.MedicoNombre = $"{turno.Medico.Apellido} {turno.Medico.Nombre}";
            model.FechaHora = turno.FechaHoraInicio;
            return View(model);
        }

        turno.PacienteId = model.PacienteId;
        turno.MotivoConsulta = string.IsNullOrWhiteSpace(model.MotivoConsulta)
            ? "Consulta"
            : model.MotivoConsulta;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Turnos/CambiarEstado/5
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Medico)
            .Include(t => t.Paciente)
            .FirstOrDefaultAsync(t => t.TurnoId == id);

        if (turno == null)
        {
            return NotFound();
        }

        var model = new CambiarEstadoTurnoViewModel
        {
            TurnoId = turno.TurnoId,
            Estado = turno.Estado,
            MedicoNombre = $"{turno.Medico.Apellido} {turno.Medico.Nombre}",
            PacienteNombre = turno.Paciente != null ? $"{turno.Paciente.Apellido} {turno.Paciente.Nombre}" : null,
            FechaHora = turno.FechaHoraInicio
        };

        return View(model);
    }

    // POST: /Turnos/CambiarEstado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(CambiarEstadoTurnoViewModel model)
    {
        var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.TurnoId == model.TurnoId);
        if (turno == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        turno.Estado = model.Estado;

        // Si el turno se marca como Atendido, crear (si no existe) la consulta médica asociada
        if (model.Estado == EstadoTurno.Atendido && turno.PacienteId.HasValue)
        {
            if (!turno.ConsultaMedicaId.HasValue)
            {
                var consulta = new ConsultaMedica
                {
                    PacienteId = turno.PacienteId.Value,
                    MedicoId = turno.MedicoId,
                    FechaConsulta = turno.FechaHoraInicio,
                    MotivoConsulta = turno.MotivoConsulta ?? "Consulta"
                };

                _context.ConsultasMedicas.Add(consulta);
                await _context.SaveChangesAsync();

                turno.ConsultaMedicaId = consulta.ConsultaMedicaId;
            }

            await _context.SaveChangesAsync();

            // Redirigir a la edición de la consulta para completar la historia clínica
            return RedirectToAction("Edit", "Consultas", new { id = turno.ConsultaMedicaId });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
