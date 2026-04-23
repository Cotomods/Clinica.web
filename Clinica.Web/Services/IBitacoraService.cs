namespace Clinica.Web.Services;

public interface IBitacoraService
{
    Task RegistrarAccionAsync(string usuario, string accion, string? detalle = null);
}
