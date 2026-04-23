using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
using Clinica.Web.Services;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Web.Controllers;

[Authorize(Roles = "Admin,RecursosHumanos")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ClinicaDbContext _clinicaContext;
    private readonly IBitacoraService _bitacora;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ClinicaDbContext clinicaContext,
        IBitacoraService bitacora)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _clinicaContext = clinicaContext;
        _bitacora = bitacora;
    }

    // GET: /Users
    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
    {
        const int pageSize = 20;
        ViewData["CurrentFilter"] = searchTerm;

        IQueryable<ApplicationUser> usersQuery = _clinicaContext.Users
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            usersQuery = usersQuery.Where(u => u.Email != null && u.Email.Contains(searchTerm));
        }

        // RRHH no debe ver usuarios con rol Admin: lo filtramos a nivel query para que la paginación sea exacta.
        if (!User.IsInRole("Admin"))
        {
            var adminRoleId = await _clinicaContext.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(adminRoleId))
            {
                usersQuery = usersQuery.Where(u =>
                    !_clinicaContext.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));
            }
        }

        usersQuery = usersQuery.OrderBy(u => u.Email);

        var totalCount = await usersQuery.CountAsync();
        if (pageNumber < 1) pageNumber = 1;

        var usersPage = await usersQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Cargamos el nombre de médico solo para los usuarios de la página
        var medicoIds = usersPage
            .Where(u => u.MedicoId.HasValue)
            .Select(u => u.MedicoId!.Value)
            .Distinct()
            .ToList();

        var medicos = await _clinicaContext.Medicos
            .AsNoTracking()
            .Where(m => medicoIds.Contains(m.MedicoId))
            .ToDictionaryAsync(m => m.MedicoId, m => $"{m.Apellido} {m.Nombre}");

        var items = new List<UserListViewModel>();

        foreach (var user in usersPage)
        {
            var roles = await _userManager.GetRolesAsync(user);

            medicos.TryGetValue(user.MedicoId ?? 0, out var medicoNombre);

            items.Add(new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                MedicoId = user.MedicoId,
                MedicoNombre = medicoNombre,
                Roles = roles.ToList()
            });
        }

        var model = new PaginatedList<UserListViewModel>(items, totalCount, pageNumber, pageSize);
        return View(model);
    }

    // GET: /Users/Create
    public async Task<IActionResult> Create()
    {
        await LoadRolesAndMedicosAsync();
        return View(new UserCreateViewModel());
    }

    // POST: /Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        // Limpieza de datos
        model.Email = model.Email?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadRolesAndMedicosAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            MedicoId = model.MedicoId
        };

        try
        {
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    // Mostrar el error debajo del campo correspondiente cuando sea posible
                    if (error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.Password), error.Description);
                    }
                    else if (string.Equals(error.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo electrónico.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                await LoadRolesAndMedicosAsync();
                return View(model);
            }

            // Asignar rol seleccionado (solo uno por usuario)
            if (model.SelectedRoles != null && model.SelectedRoles.Any())
            {
                var selectedRole = model.SelectedRoles.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(selectedRole))
                {
                    // Si NO es Admin, no puede asignar rol Admin
                    if (!User.IsInRole("Admin") && selectedRole == "Admin")
                    {
                        selectedRole = null;
                    }

                    if (!string.IsNullOrWhiteSpace(selectedRole))
                    {
                        var validRole = await _roleManager.Roles
                            .Where(r => r.Name == selectedRole)
                            .Select(r => r.Name!)
                            .FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(validRole))
                        {
                            await _userManager.AddToRoleAsync(user, validRole);
                        }
                    }
                }
            }

            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Creó un usuario", $"Email: {model.Email}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al intentar crear el usuario. Por favor, intente nuevamente.");
            await LoadRolesAndMedicosAsync();
            return View(model);
        }
    }

    // GET: /Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        // RRHH no puede editar usuarios con rol Admin
        if (!User.IsInRole("Admin") && userRoles.Contains("Admin"))
        {
            return Forbid();
        }

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            MedicoId = user.MedicoId,
            SelectedRoles = userRoles.ToList()
        };

        await LoadRolesAndMedicosAsync(user.MedicoId, userRoles.ToList());
        return View(model);
    }

    // POST: /Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        // Limpieza de datos
        model.Email = model.Email?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadRolesAndMedicosAsync(model.MedicoId, model.SelectedRoles);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (!User.IsInRole("Admin") && userRoles.Contains("Admin"))
        {
            return Forbid();
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.MedicoId = model.MedicoId;

        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (string.Equals(error.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.Email), "Ya existe otro usuario con este correo electrónico.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                await LoadRolesAndMedicosAsync(model.MedicoId, model.SelectedRoles);
                return View(model);
            }

            // Actualizar rol (solo uno por usuario)
            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.SelectedRoles ?? new List<string>();

            // Tomar solo un rol seleccionado (si hay varios por algún motivo)
            string? selectedRole = selectedRoles.FirstOrDefault();

            // Si NO es Admin, no puede tocar el rol Admin
            if (!User.IsInRole("Admin"))
            {
                if (selectedRole == "Admin")
                {
                    selectedRole = null;
                }

                // Tampoco puede quitar Admin si el usuario ya lo tiene
                if (currentRoles.Contains("Admin"))
                {
                    // Forzamos a que Admin siga estando entre los roles actuales
                    selectedRole = "Admin";
                }
            }

            var newRoles = new List<string>();
            if (!string.IsNullOrWhiteSpace(selectedRole))
            {
                newRoles.Add(selectedRole);
            }

            var rolesToAdd = newRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(newRoles).ToList();

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Editó un usuario", $"Email: {model.Email}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el usuario. Por favor, intente nuevamente.");
            await LoadRolesAndMedicosAsync(model.MedicoId, model.SelectedRoles);
            return View(model);
        }
    }

    // GET: /Users/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        // RRHH no puede eliminar usuarios con rol Admin
        if (!User.IsInRole("Admin") && roles.Contains("Admin"))
        {
            return Forbid();
        }

        var vm = new UserListViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            MedicoId = user.MedicoId,
            Roles = roles.ToList()
        };

        return View(vm);
    }

    // POST: /Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!User.IsInRole("Admin") && roles.Contains("Admin"))
        {
            return Forbid();
        }

        var emailEliminado = user.Email;

        try
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var vm = new UserListViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    MedicoId = user.MedicoId,
                    Roles = roles.ToList()
                };
                return View(vm);
            }

            await _bitacora.RegistrarAccionAsync(User.Identity?.Name ?? "Sistema", "Eliminó un usuario", $"Email: {emailEliminado}");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar eliminar el usuario.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task LoadRolesAndMedicosAsync(int? selectedMedicoId = null, IList<string>? selectedRoles = null)
    {
        var allRoles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync();

        // Si no es Admin, no mostrar rol Admin en el formulario
        if (!User.IsInRole("Admin"))
        {
            allRoles = allRoles.Where(r => r != "Admin").ToList();
        }

        ViewBag.AllRoles = allRoles;
        ViewBag.SelectedRoles = selectedRoles ?? new List<string>();

        var medicos = await _clinicaContext.Medicos
            .AsNoTracking()
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .Select(m => new
            {
                m.MedicoId,
                Nombre = $"{m.Apellido} {m.Nombre}"
            })
            .ToListAsync();

        ViewBag.MedicoId = new SelectList(medicos, "MedicoId", "Nombre", selectedMedicoId);
    }
}
