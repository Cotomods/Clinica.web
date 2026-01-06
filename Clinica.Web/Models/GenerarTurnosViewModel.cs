using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class GenerarTurnosViewModel
{
    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    [Display(Name = "Hora de inicio")]
    public TimeSpan HoraInicio { get; set; } = new TimeSpan(9, 0, 0);

    [DataType(DataType.Time)]
    [Display(Name = "Hora de fin")]
    public TimeSpan HoraFin { get; set; } = new TimeSpan(12, 0, 0);

    [Display(Name = "Duración (minutos)")]
    public int DuracionMinutos { get; set; } = 30;
}
