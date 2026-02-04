using System.ComponentModel.DataAnnotations;

namespace Clinica.Domain.Entities;

public class Paciente : IValidatableObject
{
    public int PacienteId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Display(Name = "Tipo de documento")]
    [StringLength(10, ErrorMessage = "El tipo de documento no puede superar los 10 caracteres.")]
    public string? TipoDocumento { get; set; }

    [Display(Name = "Número de documento")]
    [StringLength(30, ErrorMessage = "El número de documento no puede superar los 30 caracteres.")]
    public string? NumeroDocumento { get; set; }

    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    [Display(Name = "Teléfono")]
    // Validación flexible: permite dígitos, espacios, +, -, paréntesis
    [RegularExpression(@"^[0-9+\-\s\(\)]*$", ErrorMessage = "El teléfono solo puede contener números, espacios y los símbolos + - ( ).")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string? Email { get; set; }

    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Nº Historia Clínica")]
    [StringLength(50, ErrorMessage = "El número de historia clínica no puede superar los 50 caracteres.")]
    public string? NumeroHistoriaClinica { get; set; }

    public int? ObraSocialId { get; set; }
    public ObraSocial? ObraSocial { get; set; }

    public int? PlanObraSocialId { get; set; }
    public PlanObraSocial? PlanObraSocial { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    public ICollection<ConsultaMedica> Consultas { get; set; } = new List<ConsultaMedica>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(TipoDocumento) && string.IsNullOrWhiteSpace(NumeroDocumento))
        {
            yield return new ValidationResult(
                "El número de documento es obligatorio cuando se indica el tipo de documento.",
                new[] { nameof(NumeroDocumento) });
        }

        if (!string.IsNullOrWhiteSpace(NumeroDocumento) && string.IsNullOrWhiteSpace(TipoDocumento))
        {
            yield return new ValidationResult(
                "El tipo de documento es obligatorio cuando se indica el número de documento.",
                new[] { nameof(TipoDocumento) });
        }

        // DNI solo numérico
        if (!string.IsNullOrWhiteSpace(TipoDocumento)
            && string.Equals(TipoDocumento.Trim(), "DNI", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(NumeroDocumento)
            && !NumeroDocumento.All(char.IsDigit))
        {
            yield return new ValidationResult(
                "El DNI debe contener solo números.",
                new[] { nameof(NumeroDocumento) });
        }

        if (FechaNacimiento.HasValue && FechaNacimiento.Value.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "La fecha de nacimiento no puede ser futura.",
                new[] { nameof(FechaNacimiento) });
        }
    }
}
