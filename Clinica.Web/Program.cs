using System.Text.Json;
using Clinica.Infrastructure.Data;
using Clinica.Web.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar licencia QuestPDF (Community)
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseInMemoryDatabase("ClinicaDb"));

// DbContext para Identity (usuarios y roles)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("ClinicaIdentity"));

// Identity + Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Exigir usuario autenticado por defecto en controladores MVC
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

// Seed de datos desde JSON (solo para desarrollo/pruebas) y roles/usuarios de Identity
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Seed de datos de dominio (pacientes y médicos)
    var context = services.GetRequiredService<ClinicaDbContext>();
    if (!context.Pacientes.Any() && !context.Medicos.Any())
    {
        var dataPath = Path.Combine(app.Environment.ContentRootPath, "Data", "seed.json");
        if (File.Exists(dataPath))
        {
            var json = File.ReadAllText(dataPath);
            var seed = JsonSerializer.Deserialize<Clinica.Web.Models.SeedData>(json);
            if (seed != null)
            {
                context.Pacientes.AddRange(seed.Pacientes);
                context.Medicos.AddRange(seed.Medicos);
                context.SaveChanges();
            }
        }
    }

    // Seed de roles y usuario administrador inicial
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    SeedIdentity(roleManager, userManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ruta corta para el calendario de turnos: /Calendario
app.MapControllerRoute(
    name: "calendario",
    pattern: "Calendario/{medicoId?}",
    defaults: new { controller = "Calendario", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// Seed de roles y usuario admin (sincrónico para usar en Program.cs)
static void SeedIdentity(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    string[] roles = { "Admin", "Medico", "RecursosHumanos", "Recepcionista" };

    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
    }

    // Crear usuario administrador inicial si no existe
    var adminEmail = "admin@clinica.local";
    var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = userManager.CreateAsync(adminUser, "Admin123!").GetAwaiter().GetResult();
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
        }
    }
}
