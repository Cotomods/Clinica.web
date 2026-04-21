namespace Clinica.Domain.Entities;

public class ObraSocial
{
    public int ObraSocialId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }

    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
}
