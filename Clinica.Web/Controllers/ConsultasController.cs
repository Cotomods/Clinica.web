using Clinica.Domain.Entities;
using Clinica.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

public class ConsultasController : Controller
{
    private readonly ClinicaDbContext _context;

    public ConsultasController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Consultas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var consulta = await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == id);

        if (consulta == null)
        {
            return NotFound();
        }

        return View(consulta);
    }

    // POST: /Consultas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConsultaMedica consulta)
    {
        var existing = await _context.ConsultasMedicas
            .FirstOrDefaultAsync(c => c.ConsultaMedicaId == consulta.ConsultaMedicaId);

        if (existing == null)
        {
            return NotFound();
        }

        // Copiamos manualmente solo los campos editables; preservamos FechaConsulta y relaciones
        existing.MotivoConsulta = consulta.MotivoConsulta;
        existing.Anamnesis = consulta.Anamnesis;
        existing.ExamenFisico = consulta.ExamenFisico;
        existing.Indicaciones = consulta.Indicaciones;
        existing.NotasInternas = consulta.NotasInternas;

        await _context.SaveChangesAsync();

        var pacienteId = existing.PacienteId;
        return RedirectToAction("HistoriaClinica", "Pacientes", new { id = pacienteId });
    }
}
