using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;

namespace Clinica.Web.Services;

public class BitacoraService : IBitacoraService
{
    private readonly ClinicaDbContext _context;

    public BitacoraService(ClinicaDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAccionAsync(string usuario, string accion, string? detalle = null)
    {
        var registro = new Bitacora
        {
            Usuario = usuario,
            Accion = accion,
            Detalle = detalle,
            Fecha = DateTime.Now
        };

        _context.Bitacoras.Add(registro);
        await _context.SaveChangesAsync();
    }
}
