using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class MedicoCreateViewModel
{
    [Required]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Matrícula")]
    public string Matricula { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Especialidad")]
    public string? EspecialidadNombre { get; set; }
}
