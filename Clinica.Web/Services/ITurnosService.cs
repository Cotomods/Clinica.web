using Clinica.Domain.Entities;
using Clinica.Web.Models;

namespace Clinica.Web.Services;

public interface ITurnosService
{
    Task<List<Turno>> GenerarTurnosAsync(GenerarTurnosViewModel model);
    Task<int?> CambiarEstadoAsync(int turnoId, EstadoTurno nuevoEstado, string? motivoConsulta = null);
}
