using System.ComponentModel.DataAnnotations;

namespace Clinica.Domain.Entities;

public class ObraSocial
{
    public int ObraSocialId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    public string? Codigo { get; set; }

    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
}
