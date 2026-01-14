namespace Clinica.Domain.Entities;

public enum EstadoTurno
{
    Reservado = 0,
    Confirmado = 1,
    Atendido = 2,
    Cancelado = 3,
    Ausente = 4
}

public class Turno
{
    public int TurnoId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Reservado;
    public string? MotivoConsulta { get; set; }

    public int? PacienteId { get; set; }
    public Paciente? Paciente { get; set; } = null!;

    public int MedicoId { get; set; }
    public Medico Medico { get; set; } = null!;

    public int? ConsultorioId { get; set; }
    public Consultorio? Consultorio { get; set; }

    public int? ConsultaMedicaId { get; set; }
    public ConsultaMedica? ConsultaMedica { get; set; }
}
