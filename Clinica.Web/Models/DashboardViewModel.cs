using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class DashboardViewModel
{
    public int TotalPacientes { get; set; }
    public int TotalMedicos { get; set; }
    public int TotalObrasSociales { get; set; }
    public int TurnosHoyTotal { get; set; }
    public int TurnosHoyLibres { get; set; }
    public int TurnosHoyAtendidos { get; set; }
    public int TurnosHoyCancelados { get; set; }
    public int TurnosHoyAusentes { get; set; }

    public DateTime FechaHoy { get; set; }

    /// <summary>Próximos turnos asignados (con paciente, aún no atendidos)</summary>
    public List<Turno> ProximosTurnos { get; set; } = new();
    /// <summary>Todos los turnos del día</summary>
    public List<Turno> TurnosHoy { get; set; } = new();
}
