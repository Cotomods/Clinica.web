using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data;

public static class DbInitializer
{
    public static void SeedData(ClinicaDbContext context)
    {
        // No se generan datos de prueba por solicitud del usuario.
        // Solo se creará el usuario administrador y los roles iniciales (gestionado en Program.cs).
    }
}
