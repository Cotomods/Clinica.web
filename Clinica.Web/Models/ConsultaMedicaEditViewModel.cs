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
    [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
    [StringLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string MotivoConsulta { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "La anamnesis no puede superar los 4000 caracteres.")]
    public string? Anamnesis { get; set; }

    [Display(Name = "Examen físico")]
    [StringLength(4000, ErrorMessage = "El examen físico no puede superar los 4000 caracteres.")]
    public string? ExamenFisico { get; set; }

    [StringLength(4000, ErrorMessage = "Las indicaciones no pueden superar los 4000 caracteres.")]
    public string? Indicaciones { get; set; }

    [Display(Name = "Notas internas")]
    [StringLength(4000, ErrorMessage = "Las notas internas no pueden superar los 4000 caracteres.")]
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
