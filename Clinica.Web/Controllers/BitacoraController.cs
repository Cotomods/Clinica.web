using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize(Roles = "Admin")]
public class BitacoraController : Controller
{
    private readonly ClinicaDbContext _context;

    public BitacoraController(ClinicaDbContext context)
    {
        _context = context;
    }

    // GET: /Bitacora
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        const int pageSize = 20;
        ViewData["CurrentFilter"] = searchTerm;

        var query = _context.Bitacoras.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(b =>
                b.Usuario.Contains(searchTerm) ||
                b.Accion.Contains(searchTerm) ||
                (b.Detalle != null && b.Detalle.Contains(searchTerm)));
        }

        query = query.OrderByDescending(b => b.Fecha);

        var model = await PaginatedList<Clinica.Domain.Entities.Bitacora>
            .CreateAsync(query, pageNumber, pageSize);

        return View(model);
    }
}
