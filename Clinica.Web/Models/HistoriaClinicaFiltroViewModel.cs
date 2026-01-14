using System.ComponentModel.DataAnnotations;
using Clinica.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Clinica.Web.Models;

public class HistoriaClinicaFiltroViewModel
{
    public Paciente Paciente { get; set; } = null!;

    public List<ConsultaMedica> Consultas { get; set; } = new();

    [DataType(DataType.Date)]
    [Display(Name = "Desde")]
    public DateTime? FechaDesde { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hasta")]
    public DateTime? FechaHasta { get; set; }

    [Display(Name = "Médico")]
    public int? MedicoId { get; set; }

    public IEnumerable<SelectListItem> Medicos { get; set; } = Enumerable.Empty<SelectListItem>();
}
