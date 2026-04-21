using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class MedicoEditViewModel : MedicoCreateViewModel
{
    [Required]
    public int MedicoId { get; set; }
}
