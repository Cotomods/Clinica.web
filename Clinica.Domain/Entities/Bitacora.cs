using System.ComponentModel.DataAnnotations;

namespace Clinica.Domain.Entities;

public class Bitacora
{
    public int BitacoraId { get; set; }

    [Required]
    [StringLength(256)]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Acción")]
    public string Accion { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Detalle")]
    public string? Detalle { get; set; }

    [Display(Name = "Fecha")]
    [DataType(DataType.DateTime)]
    public DateTime Fecha { get; set; }
}
