using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class AsignarTurnoViewModel
{
    public int TurnoId { get; set; }

    [Display(Name = "Paciente")]
    [Required]
    public int? PacienteId { get; set; }

    [Display(Name = "Motivo de consulta")]
    public string? MotivoConsulta { get; set; }

    public string MedicoNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
