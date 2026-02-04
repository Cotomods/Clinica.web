using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Clinica.Web.Models;

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? MedicoId { get; set; }
    public string? MedicoNombre { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UserCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "La contraseña debe tener al menos: una minúscula, una mayúscula, un número y un símbolo.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public int? MedicoId { get; set; }

    // Roles seleccionados en el formulario (se usa radio, pero se bindea a lista)
    [MinLength(1, ErrorMessage = "Debe seleccionar un rol.")]
    public List<string> SelectedRoles { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var role = SelectedRoles?.FirstOrDefault();
        if (string.Equals(role, "Medico", StringComparison.OrdinalIgnoreCase) && MedicoId == null)
        {
            yield return new ValidationResult(
                "Si el usuario tiene rol Médico, debe asociarse a un médico.",
                new[] { nameof(MedicoId) });
        }
    }
}

public class UserEditViewModel : IValidatableObject
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    public int? MedicoId { get; set; }

    [MinLength(1, ErrorMessage = "Debe seleccionar un rol.")]
    public List<string> SelectedRoles { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var role = SelectedRoles?.FirstOrDefault();
        if (string.Equals(role, "Medico", StringComparison.OrdinalIgnoreCase) && MedicoId == null)
        {
            yield return new ValidationResult(
                "Si el usuario tiene rol Médico, debe asociarse a un médico.",
                new[] { nameof(MedicoId) });
        }
    }
}
