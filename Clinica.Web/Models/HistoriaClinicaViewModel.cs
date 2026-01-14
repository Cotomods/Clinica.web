using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class HistoriaClinicaViewModel
{
    public Paciente Paciente { get; set; } = null!;
    public List<ConsultaMedica> Consultas { get; set; } = new();
}
