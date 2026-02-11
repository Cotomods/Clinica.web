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
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        const int pageSize = 20;
        ViewData["CurrentFilter"] = searchTerm;

        var query = _context.Medicos
            .Include(m => m.Especialidad)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(m => m.Apellido.Contains(searchTerm) || (m.Especialidad != null && m.Especialidad.Nombre.Contains(searchTerm)));
        }

        query = query
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre);

        var medicos = await PaginatedList<Medico>.CreateAsync(query, pageNumber, pageSize);
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
            FechaDesde = DateTime.Today,
            FechaHasta = DateTime.Today.AddDays(14),
            DuracionMinutos = 30,
            HorariosPorDia = BuildDefaultHorariosPorDia()
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

        if (!ModelState.IsValid)
        {
            model.MedicoNombre = $"{medico.Apellido} {medico.Nombre}";
            model.HorariosPorDia = NormalizeHorariosPorDia(model.HorariosPorDia);
            return View(model);
        }

        // Seguridad extra: no permitir generar en fechas pasadas (además de la validación del ViewModel)
        if (model.FechaDesde.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.FechaDesde), "No se pueden generar turnos en fechas pasadas.");
            model.MedicoNombre = $"{medico.Apellido} {medico.Nombre}";
            model.HorariosPorDia = NormalizeHorariosPorDia(model.HorariosPorDia);
            return View(model);
        }

        var desde = model.FechaDesde.Date;
        var hasta = model.FechaHasta.Date;
        var endExclusive = hasta.AddDays(1);

        var horariosPorDia = NormalizeHorariosPorDia(model.HorariosPorDia)
            .Where(h => h.Atiende)
            .ToDictionary(h => h.Dia, h => h);

        // Obtenemos los horarios ya existentes para ese médico en el rango completo
        var existentes = await _context.Turnos
            .Where(t => t.MedicoId == medico.MedicoId && t.FechaHoraInicio >= desde && t.FechaHoraInicio < endExclusive)
            .Select(t => t.FechaHoraInicio)
            .ToListAsync();

        var existentesSet = existentes.ToHashSet();

        var turnos = new List<Turno>();
        var now = DateTime.Now;

        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            if (!horariosPorDia.TryGetValue(fecha.DayOfWeek, out var h))
            {
                continue;
            }

            // Si el día está marcado como atención, pero faltan horas (por seguridad)
            if (!h.HoraInicio.HasValue || !h.HoraFin.HasValue)
            {
                continue;
            }

            var inicioDia = fecha.Date + h.HoraInicio.Value;
            var finDia = fecha.Date + h.HoraFin.Value;

            for (var actual = inicioDia; actual < finDia; actual = actual.AddMinutes(model.DuracionMinutos))
            {
                // No crear turnos en el pasado
                if (actual < now)
                {
                    continue;
                }

                // Si ya existe un turno con este horario, lo saltamos
                if (existentesSet.Contains(actual))
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
                existentesSet.Add(actual); // evita duplicados dentro de la misma corrida
            }
        }

        if (turnos.Count == 0)
        {
            ModelState.AddModelError(string.Empty,
                "No hay turnos para generar en el rango seleccionado (todos están en el pasado, no coinciden con los días configurados o ya existen)."
            );
            model.MedicoNombre = $"{medico.Apellido} {medico.Nombre}";
            model.HorariosPorDia = NormalizeHorariosPorDia(model.HorariosPorDia);
            return View(model);
        }

        _context.Turnos.AddRange(turnos);
        await _context.SaveChangesAsync();

        // Redirigir al calendario de turnos luego de generar la agenda
        return RedirectToAction("Index", "Calendario");
    }

    private static List<DiaAtencionHorarioViewModel> BuildDefaultHorariosPorDia()
    {
        return new List<DiaAtencionHorarioViewModel>
        {
            new() { Dia = DayOfWeek.Monday, Atiende = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Tuesday, Atiende = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Wednesday, Atiende = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Thursday, Atiende = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Friday, Atiende = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Saturday, Atiende = false, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
            new() { Dia = DayOfWeek.Sunday, Atiende = false, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
        };
    }

    private static List<DiaAtencionHorarioViewModel> NormalizeHorariosPorDia(List<DiaAtencionHorarioViewModel>? items)
    {
        // Garantiza que existan los 7 días, en orden fijo, para que la vista siempre renderice igual.
        var baseList = items ?? new List<DiaAtencionHorarioViewModel>();
        var dict = baseList
            .GroupBy(i => i.Dia)
            .ToDictionary(g => g.Key, g => g.First());

        var ordered = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        var result = new List<DiaAtencionHorarioViewModel>();
        foreach (var d in ordered)
        {
            if (dict.TryGetValue(d, out var item))
            {
                result.Add(item);
            }
            else
            {
                result.Add(new DiaAtencionHorarioViewModel
                {
                    Dia = d,
                    Atiende = d is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                });
            }
        }

        return result;
    }
}
