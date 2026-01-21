using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class CalendarioTurnosViewModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public DateTime FechaSeleccionada { get; set; }

    public int? MedicoId { get; set; }

    public int? PacienteId { get; set; }

    public List<int> DiasConTurnos { get; set; } = new();
    public List<Turno> TurnosDelDia { get; set; } = new();
}
