using System.ComponentModel.DataAnnotations;
using Clinica.Domain.Entities;

namespace Clinica.Web.Models;

public class ConsultaMedicaEditViewModel
{
    public int ConsultaMedicaId { get; set; }

    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;

    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;

    public DateTime FechaConsulta { get; set; }

    [Display(Name = "Motivo de consulta")]
    public string MotivoConsulta { get; set; } = string.Empty;

    public string? Anamnesis { get; set; }
    public string? ExamenFisico { get; set; }
    public string? Indicaciones { get; set; }
    public string? NotasInternas { get; set; }

    public List<Diagnostico> Diagnosticos { get; set; } = new();
    public List<Receta> Recetas { get; set; } = new();

    // Campos para alta rápida de diagnóstico
    [Display(Name = "Código diagnóstico")]
    public string? NuevoDiagnosticoCodigo { get; set; }

    [Display(Name = "Descripción diagnóstico")]
    public string? NuevoDiagnosticoDescripcion { get; set; }

    // Campos para alta rápida de receta
    [Display(Name = "Medicamento")]
    public string? NuevaRecetaMedicamento { get; set; }

    [Display(Name = "Dosis")]
    public string? NuevaRecetaDosis { get; set; }

    [Display(Name = "Frecuencia")]
    public string? NuevaRecetaFrecuencia { get; set; }

    [Display(Name = "Duración")]
    public string? NuevaRecetaDuracion { get; set; }
}
