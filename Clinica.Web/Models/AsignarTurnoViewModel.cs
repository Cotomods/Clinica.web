using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class AsignarTurnoViewModel
{
    public int TurnoId { get; set; }

    [Display(Name = "Paciente")]
    [Required(ErrorMessage = "Debe seleccionar un paciente.")]
    public int? PacienteId { get; set; }

    [Display(Name = "Motivo de consulta")]
    [StringLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string? MotivoConsulta { get; set; }

    public string MedicoNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
