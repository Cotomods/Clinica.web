using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class DashboardViewModel
{
    public int TotalPacientes { get; set; }
    public int TotalMedicos { get; set; }
    public int TurnosHoyTotal { get; set; }
    public int TurnosHoyLibres { get; set; }

    public DateTime FechaHoy { get; set; }

    public List<Turno> TurnosHoy { get; set; } = new();
}
