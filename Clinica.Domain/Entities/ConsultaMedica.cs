namespace Clinica.Domain.Entities;

public class ConsultaMedica
{
    public int ConsultaMedicaId { get; set; }
    public DateTime FechaConsulta { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? Anamnesis { get; set; }
    public string? ExamenFisico { get; set; }
    public string? Indicaciones { get; set; }
    public string? NotasInternas { get; set; }

    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public int MedicoId { get; set; }
    public Medico Medico { get; set; } = null!;

    public ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();
}
