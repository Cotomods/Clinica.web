using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class SeedData
{
    public List<Paciente> Pacientes { get; set; } = new();
    public List<Medico> Medicos { get; set; } = new();
}
