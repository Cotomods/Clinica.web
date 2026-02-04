using System.ComponentModel.DataAnnotations;

namespace Clinica.Domain.Entities;

public enum EstadoTurno
{
    Reservado = 0,
    Confirmado = 1,
    Atendido = 2,
    Cancelado = 3,
    Ausente = 4
}

public class Turno : IValidatableObject
{
    public int TurnoId { get; set; }

    [Display(Name = "Inicio")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHoraInicio { get; set; }

    [Display(Name = "Fin")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHoraFin { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.Reservado;

    [Display(Name = "Motivo de consulta")]
    [StringLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string? MotivoConsulta { get; set; }

    public int? PacienteId { get; set; }
    public Paciente? Paciente { get; set; } = null!;

    public int MedicoId { get; set; }
    public Medico Medico { get; set; } = null!;

    public int? ConsultorioId { get; set; }
    public Consultorio? Consultorio { get; set; }

    public int? ConsultaMedicaId { get; set; }
    public ConsultaMedica? ConsultaMedica { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaHoraFin <= FechaHoraInicio)
        {
            yield return new ValidationResult(
                "La fecha/hora de fin debe ser mayor a la de inicio.",
                new[] { nameof(FechaHoraFin) });
        }
    }
}
