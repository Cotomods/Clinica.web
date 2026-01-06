using System.ComponentModel.DataAnnotations;
using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class CambiarEstadoTurnoViewModel
{
    public int TurnoId { get; set; }

    [Display(Name = "Estado")]
    public EstadoTurno Estado { get; set; }

    public string MedicoNombre { get; set; } = string.Empty;
    public string? PacienteNombre { get; set; }
    public DateTime FechaHora { get; set; }
}
