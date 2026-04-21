using Microsoft.AspNetCore.Identity;

namespace Clinica.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    // Si el usuario representa a un médico, se vincula con la entidad Medico del dominio
    public int? MedicoId { get; set; }
}
