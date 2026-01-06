namespace Clinica.Domain.Entities;

public class Diagnostico
{
    public int DiagnosticoId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public int ConsultaMedicaId { get; set; }
    public ConsultaMedica ConsultaMedica { get; set; } = null!;
}
