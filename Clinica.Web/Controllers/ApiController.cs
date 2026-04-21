using Clinica.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

/// <summary>
/// Endpoints JSON para Select2 autocomplete.
/// Devuelve resultados en formato { results: [{ id, text }], pagination: { more } }.
/// </summary>
[Authorize]
[Route("api")]
public class ApiController : Controller
{
    private readonly ClinicaDbContext _context;
    private const int PageSize = 20;

    public ApiController(ClinicaDbContext context)
    {
        _context = context;
    }

    [HttpGet("pacientes")]
    public async Task<IActionResult> BuscarPacientes(string? term, int page = 1)
    {
        var query = _context.Pacientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();
            query = query.Where(p =>
                p.Apellido.Contains(term)
                || p.Nombre.Contains(term)
                || (p.NumeroDocumento != null && p.NumeroDocumento.Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new
            {
                id = p.PacienteId,
                text = p.Apellido + " " + p.Nombre
                       + (p.NumeroDocumento != null ? " — " + p.TipoDocumento + " " + p.NumeroDocumento : "")
            })
            .ToListAsync();

        return Json(new
        {
            results = items,
            pagination = new { more = (page * PageSize) < total }
        });
    }

    [HttpGet("medicos")]
    public async Task<IActionResult> BuscarMedicos(string? term, int page = 1)
    {
        var query = _context.Medicos
            .Include(m => m.Especialidad)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();
            query = query.Where(m =>
                m.Apellido.Contains(term)
                || m.Nombre.Contains(term)
                || (m.Especialidad != null && m.Especialidad.Nombre.Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new
            {
                id = m.MedicoId,
                text = m.Apellido + " " + m.Nombre
                       + (m.Especialidad != null ? " — " + m.Especialidad.Nombre : "")
            })
            .ToListAsync();

        return Json(new
        {
            results = items,
            pagination = new { more = (page * PageSize) < total }
        });
    }
}
