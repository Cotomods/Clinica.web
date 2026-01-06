namespace Clinica.Domain.Entities;

public class Consultorio
{
    public int ConsultorioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Piso { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
