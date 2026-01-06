namespace Clinica.Domain.Entities;

public class Medico
{
    public int MedicoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;

    public int EspecialidadId { get; set; }
    public Especialidad Especialidad { get; set; } = null!;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    public ICollection<ConsultaMedica> Consultas { get; set; } = new List<ConsultaMedica>();
}
