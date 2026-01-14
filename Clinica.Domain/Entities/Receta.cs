namespace Clinica.Domain.Entities;

public class Receta
{
    public int RecetaId { get; set; }

    public string Medicamento { get; set; } = string.Empty;
    public string? Dosis { get; set; }
    public string? Frecuencia { get; set; }
    public string? Duracion { get; set; }

    public int ConsultaMedicaId { get; set; }
    public ConsultaMedica ConsultaMedica { get; set; } = null!;
}
