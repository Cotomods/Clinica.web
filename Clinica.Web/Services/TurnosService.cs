using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Services;

public class TurnosService : ITurnosService
{
    private readonly ClinicaDbContext _context;

    public TurnosService(ClinicaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Turno>> GenerarTurnosAsync(GenerarTurnosViewModel model)
    {
        var desde = model.FechaDesde.Date;
        var hasta = model.FechaHasta.Date;
        var endExclusive = hasta.AddDays(1);

        var horariosPorDia = model.HorariosPorDia
            .Where(h => h.Atiende)
            .ToDictionary(h => h.Dia, h => h);

        var existentes = await _context.Turnos
            .Where(t => t.MedicoId == model.MedicoId && t.FechaHoraInicio >= desde && t.FechaHoraInicio < endExclusive)
            .Select(t => new { t.FechaHoraInicio, t.FechaHoraFin })
            .ToListAsync();

        var turnos = new List<Turno>();
        var now = DateTime.Now;

        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            if (!horariosPorDia.TryGetValue(fecha.DayOfWeek, out var h)) continue;
            if (!h.HoraInicio.HasValue || !h.HoraFin.HasValue) continue;

            var inicioDia = fecha.Date + h.HoraInicio.Value;
            var finDia = fecha.Date + h.HoraFin.Value;

            for (var actual = inicioDia; actual < finDia; actual = actual.AddMinutes(model.DuracionMinutos))
            {
                if (actual < now) continue;

                var actualFin = actual.AddMinutes(model.DuracionMinutos);
                if (existentes.Any(e => actual < e.FechaHoraFin && actualFin > e.FechaHoraInicio)) continue;

                turnos.Add(new Turno
                {
                    MedicoId = model.MedicoId,
                    FechaHoraInicio = actual,
                    FechaHoraFin = actualFin,
                    Estado = EstadoTurno.Disponible
                });
            }
        }

        if (turnos.Any())
        {
            _context.Turnos.AddRange(turnos);
            await _context.SaveChangesAsync();
        }

        return turnos;
    }

    public async Task<int?> CambiarEstadoAsync(int turnoId, EstadoTurno nuevoEstado, string? motivoConsulta = null)
    {
        var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.TurnoId == turnoId);
        if (turno == null) return null;

        turno.Estado = nuevoEstado;

        if (nuevoEstado == EstadoTurno.Atendido && turno.PacienteId.HasValue)
        {
            if (!turno.ConsultaMedicaId.HasValue)
            {
                var consulta = new ConsultaMedica
                {
                    PacienteId = turno.PacienteId.Value,
                    MedicoId = turno.MedicoId,
                    FechaConsulta = turno.FechaHoraInicio,
                    MotivoConsulta = string.IsNullOrWhiteSpace(motivoConsulta) 
                        ? (string.IsNullOrWhiteSpace(turno.MotivoConsulta) ? "Consulta" : turno.MotivoConsulta) 
                        : motivoConsulta
                };

                _context.ConsultasMedicas.Add(consulta);
                await _context.SaveChangesAsync();

                turno.ConsultaMedicaId = consulta.ConsultaMedicaId;
            }
        }

        await _context.SaveChangesAsync();
        return turno.ConsultaMedicaId;
    }
}
