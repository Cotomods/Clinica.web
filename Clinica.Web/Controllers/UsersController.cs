using Clinica.Infrastructure.Data;
using Clinica.Web.Models;
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

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ClinicaDbContext clinicaContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _clinicaContext = clinicaContext;
    }

    // GET: /Users
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var medicos = await _clinicaContext.Medicos
            .AsNoTracking()
            .ToDictionaryAsync(m => m.MedicoId, m => $"{m.Apellido} {m.Nombre}");

        var model = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // RRHH no debe ver usuarios con rol Admin
            if (!User.IsInRole("Admin") && roles.Contains("Admin"))
            {
                continue;
            }

            medicos.TryGetValue(user.MedicoId ?? 0, out var medicoNombre);

            model.Add(new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                MedicoId = user.MedicoId,
                MedicoNombre = medicoNombre,
                Roles = roles.ToList()
            });
        }

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

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
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

        return RedirectToAction(nameof(Index));
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

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
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

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
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
