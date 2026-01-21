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

public class UserCreateViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public int? MedicoId { get; set; }

    // Roles seleccionados en el formulario
    public List<string> SelectedRoles { get; set; } = new();
}

public class UserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public int? MedicoId { get; set; }

    public List<string> SelectedRoles { get; set; } = new();
}
