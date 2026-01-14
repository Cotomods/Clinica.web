namespace Clinica.Domain.Entities;

public class ObraSocial
{
    public int ObraSocialId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }

    public ICollection<PlanObraSocial> Planes { get; set; } = new List<PlanObraSocial>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
}

public class PlanObraSocial
{
    public int PlanObraSocialId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public int ObraSocialId { get; set; }
    public ObraSocial ObraSocial { get; set; } = null!;

    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
}
