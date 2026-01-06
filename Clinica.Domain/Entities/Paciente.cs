namespace Clinica.Domain.Entities;

public class Paciente
{
    public int PacienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? NumeroHistoriaClinica { get; set; }

    public int? ObraSocialId { get; set; }
    public ObraSocial? ObraSocial { get; set; }

    public int? PlanObraSocialId { get; set; }
    public PlanObraSocial? PlanObraSocial { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    public ICollection<ConsultaMedica> Consultas { get; set; } = new List<ConsultaMedica>();
}
