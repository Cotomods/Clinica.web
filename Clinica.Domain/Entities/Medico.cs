using System.ComponentModel.DataAnnotations;

namespace Clinica.Domain.Entities;

public class Medico
{
    public int MedicoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [StringLength(20, ErrorMessage = "La matrícula no puede superar los 20 caracteres.")]
    public string Matricula { get; set; } = string.Empty;

    public int? EspecialidadId { get; set; }
    public Especialidad? Especialidad { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    public ICollection<ConsultaMedica> Consultas { get; set; } = new List<ConsultaMedica>();
}
