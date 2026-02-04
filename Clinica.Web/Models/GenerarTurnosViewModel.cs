using System.ComponentModel.DataAnnotations;

namespace Clinica.Web.Models;

public class DiaAtencionHorarioViewModel : IValidatableObject
{
    [Display(Name = "Día")]
    public DayOfWeek Dia { get; set; }

    [Display(Name = "Atiende")]
    public bool Atiende { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Hora inicio")]
    public TimeSpan? HoraInicio { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Hora fin")]
    public TimeSpan? HoraFin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Atiende)
        {
            yield break;
        }

        if (!HoraInicio.HasValue)
        {
            yield return new ValidationResult(
                "Debe indicar la hora de inicio.",
                new[] { nameof(HoraInicio) });
        }

        if (!HoraFin.HasValue)
        {
            yield return new ValidationResult(
                "Debe indicar la hora de fin.",
                new[] { nameof(HoraFin) });
        }

        if (HoraInicio.HasValue && HoraFin.HasValue && HoraFin.Value <= HoraInicio.Value)
        {
            yield return new ValidationResult(
                "La hora de fin debe ser mayor a la hora de inicio.",
                new[] { nameof(HoraFin) });
        }
    }
}

public class GenerarTurnosViewModel : IValidatableObject
{
    private const int MaxDiasRango = 366;

    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha desde")]
    public DateTime FechaDesde { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha hasta")]
    public DateTime FechaHasta { get; set; } = DateTime.Today;

    [Display(Name = "Horarios por día")]
    public List<DiaAtencionHorarioViewModel> HorariosPorDia { get; set; } = new();

    [Display(Name = "Duración (minutos)")]
    [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos.")]
    public int DuracionMinutos { get; set; } = 30;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaHasta.Date < FechaDesde.Date)
        {
            yield return new ValidationResult(
                "La fecha hasta debe ser mayor o igual a la fecha desde.",
                new[] { nameof(FechaHasta) });
        }

        if (FechaDesde.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "No se pueden generar turnos en fechas pasadas.",
                new[] { nameof(FechaDesde) });
        }

        var dias = (FechaHasta.Date - FechaDesde.Date).TotalDays + 1;
        if (dias > MaxDiasRango)
        {
            yield return new ValidationResult(
                $"El rango no puede superar {MaxDiasRango} días.",
                new[] { nameof(FechaHasta) });
        }

        if (HorariosPorDia == null || HorariosPorDia.Count == 0)
        {
            yield return new ValidationResult(
                "Debe configurar al menos un día de atención.",
                new[] { nameof(HorariosPorDia) });
            yield break;
        }

        if (!HorariosPorDia.Any(h => h.Atiende))
        {
            yield return new ValidationResult(
                "Debe seleccionar al menos un día de la semana.",
                new[] { nameof(HorariosPorDia) });
        }

    }
}
