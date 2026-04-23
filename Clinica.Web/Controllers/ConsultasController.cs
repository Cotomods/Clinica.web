using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clinica.Web.Services;

namespace Clinica.Web.Controllers;

[Authorize(Roles = "Admin,Medico")]
public class ConsultasController : Controller
{
    private readonly ClinicaDbContext _context;
    private readonly IBitacoraService _bitacora;

    public ConsultasController(ClinicaDbContext context, IBitacoraService bitacora)
    {
        _context = context;
        _bitacora = bitacora;
    }

    // GET: /Consultas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var consulta = await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
            .Include(c => c.Diagnosticos)
            .Include(c => c.Recetas)
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == id);

        if (consulta == null)
        {
            return NotFound();
        }

        var vm = new ConsultaMedicaEditViewModel
        {
            ConsultaMedicaId = consulta.ConsultaMedicaId,
            PacienteId = consulta.PacienteId,
            PacienteNombre = $"{consulta.Paciente.Apellido} {consulta.Paciente.Nombre}",
            MedicoId = consulta.MedicoId,
            MedicoNombre = $"{consulta.Medico.Apellido} {consulta.Medico.Nombre}",
            FechaConsulta = consulta.FechaConsulta,
            MotivoConsulta = consulta.MotivoConsulta,
            Anamnesis = consulta.Anamnesis,
            ExamenFisico = consulta.ExamenFisico,
            Indicaciones = consulta.Indicaciones,
            NotasInternas = consulta.NotasInternas,
            Diagnosticos = consulta.Diagnosticos.OrderBy(d => d.DiagnosticoId).ToList(),
            Recetas = consulta.Recetas.OrderBy(r => r.RecetaId).ToList()
        };

        return View(vm);
    }

    // POST: /Consultas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConsultaMedicaEditViewModel model)
    {
        // Limpieza de datos
        model.MotivoConsulta = model.MotivoConsulta?.Trim();
        model.Anamnesis = model.Anamnesis?.Trim();
        model.ExamenFisico = model.ExamenFisico?.Trim();
        model.Indicaciones = model.Indicaciones?.Trim();
        model.NotasInternas = model.NotasInternas?.Trim();

        var existing = await _context.ConsultasMedicas
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == model.ConsultaMedicaId);

        if (existing == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var consulta = await _context.ConsultasMedicas
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.Diagnosticos)
                .Include(c => c.Recetas)
                .FirstOrDefaultAsync(c => c.ConsultaMedicaId == model.ConsultaMedicaId);

            if (consulta == null)
            {
                return NotFound();
            }

            model.PacienteNombre = $"{consulta.Paciente.Apellido} {consulta.Paciente.Nombre}";
            model.MedicoNombre = $"{consulta.Medico.Apellido} {consulta.Medico.Nombre}";
            model.FechaConsulta = consulta.FechaConsulta;
            model.Diagnosticos = consulta.Diagnosticos.OrderBy(d => d.DiagnosticoId).ToList();
            model.Recetas = consulta.Recetas.OrderBy(r => r.RecetaId).ToList();

            return View(model);
        }

        try
        {
            // Copiamos manualmente solo los campos editables; preservamos FechaConsulta y relaciones
            existing.MotivoConsulta = model.MotivoConsulta;
            existing.Anamnesis = model.Anamnesis;
            existing.ExamenFisico = model.ExamenFisico;
            existing.Indicaciones = model.Indicaciones;
            existing.NotasInternas = model.NotasInternas;

            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Editó consulta médica", $"ConsultaId: {existing.ConsultaMedicaId} del paciente {model.PacienteNombre}");

            var pacienteId = existing.PacienteId;
            return RedirectToAction("HistoriaClinica", "Pacientes", new { id = pacienteId });
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al guardar los cambios en la consulta.");
            var consulta = await _context.ConsultasMedicas
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.Diagnosticos)
                .Include(c => c.Recetas)
                .FirstOrDefaultAsync(c => c.ConsultaMedicaId == model.ConsultaMedicaId);

            if (consulta != null)
            {
                model.PacienteNombre = $"{consulta.Paciente.Apellido} {consulta.Paciente.Nombre}";
                model.MedicoNombre = $"{consulta.Medico.Apellido} {consulta.Medico.Nombre}";
                model.FechaConsulta = consulta.FechaConsulta;
                model.Diagnosticos = consulta.Diagnosticos.OrderBy(d => d.DiagnosticoId).ToList();
                model.Recetas = consulta.Recetas.OrderBy(r => r.RecetaId).ToList();
            }
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarDiagnostico(ConsultaMedicaEditViewModel model)
    {
        var consulta = await _context.ConsultasMedicas
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == model.ConsultaMedicaId);

        if (consulta == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(model.NuevoDiagnosticoDescripcion))
        {
            try
            {
                var diag = new Diagnostico
                {
                    ConsultaMedicaId = consulta.ConsultaMedicaId,
                    Codigo = string.IsNullOrWhiteSpace(model.NuevoDiagnosticoCodigo) ? null : model.NuevoDiagnosticoCodigo.Trim(),
                    Descripcion = model.NuevoDiagnosticoDescripcion.Trim()
                };

                _context.Diagnosticos.Add(diag);
                await _context.SaveChangesAsync();
                await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Agregó diagnóstico", $"Diagnóstico '{diag.Descripcion}' a ConsultaId: {consulta.ConsultaMedicaId}");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al intentar agregar el diagnóstico.";
            }
        }

        return RedirectToAction(nameof(Edit), new { id = consulta.ConsultaMedicaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDiagnostico(int id)
    {
        var diag = await _context.Diagnosticos.FirstOrDefaultAsync(d => d.DiagnosticoId == id);
        if (diag == null)
        {
            return NotFound();
        }

        var consultaId = diag.ConsultaMedicaId;
        
        try
        {
            var descripcion = diag.Descripcion;
            _context.Diagnosticos.Remove(diag);
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Eliminó diagnóstico", $"Diagnóstico '{descripcion}' de ConsultaId: {consultaId}");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar eliminar el diagnóstico.";
        }

        return RedirectToAction(nameof(Edit), new { id = consultaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarReceta(ConsultaMedicaEditViewModel model)
    {
        var consulta = await _context.ConsultasMedicas
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == model.ConsultaMedicaId);

        if (consulta == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(model.NuevaRecetaMedicamento))
        {
            try
            {
                var receta = new Receta
                {
                    ConsultaMedicaId = consulta.ConsultaMedicaId,
                    Medicamento = model.NuevaRecetaMedicamento.Trim(),
                    Dosis = model.NuevaRecetaDosis?.Trim(),
                    Frecuencia = model.NuevaRecetaFrecuencia?.Trim(),
                    Duracion = model.NuevaRecetaDuracion?.Trim()
                };

                _context.Recetas.Add(receta);
                await _context.SaveChangesAsync();
                await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Agregó receta", $"Medicamento '{receta.Medicamento}' a ConsultaId: {consulta.ConsultaMedicaId}");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al intentar agregar la receta.";
            }
        }

        return RedirectToAction(nameof(Edit), new { id = consulta.ConsultaMedicaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarReceta(int id)
    {
        var receta = await _context.Recetas.FirstOrDefaultAsync(r => r.RecetaId == id);
        if (receta == null)
        {
            return NotFound();
        }

        var consultaId = receta.ConsultaMedicaId;
        
        try
        {
            var medicamento = receta.Medicamento;
            _context.Recetas.Remove(receta);
            await _context.SaveChangesAsync();
            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Eliminó receta", $"Medicamento '{medicamento}' de ConsultaId: {consultaId}");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Ocurrió un error al intentar eliminar la receta.";
        }

        return RedirectToAction(nameof(Edit), new { id = consultaId });
    }
}
