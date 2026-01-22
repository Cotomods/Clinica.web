using Clinica.Infrastructure.Data;
using Clinica.Web.Data;
using Clinica.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar licencia QuestPDF (Community)
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicaDb")));

// DbContext para Identity (usuarios y roles) usando la misma base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicaDb")));

// Identity + Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configurar ruta de login personalizada y evitar exponer las páginas de registro/recuperación de contraseña
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

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

// Inicializar base de datos (crear si no existe) + aplicar migraciones + seed de roles/usuario admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    EnsureDatabaseCreatedAndMigrated<ClinicaDbContext>(services, logger);
    EnsureDatabaseCreatedAndMigrated<ApplicationDbContext>(services, logger);

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

// Bloquear directamente las URLs de registro y recuperación de contraseña de Identity
app.MapGet("/Identity/Account/Register", () => Results.NotFound());
app.MapPost("/Identity/Account/Register", () => Results.NotFound());
app.MapGet("/Identity/Account/ForgotPassword", () => Results.NotFound());
app.MapPost("/Identity/Account/ForgotPassword", () => Results.NotFound());

app.Run();

// Crea la base de datos si no existe (cuando el provider es relacional) y luego aplica migraciones.
// Nota: si el servidor no es accesible o no hay permisos para crear DB, esto lanzará excepción.
static void EnsureDatabaseCreatedAndMigrated<TContext>(IServiceProvider services, ILogger logger)
    where TContext : DbContext
{
    var db = services.GetRequiredService<TContext>();

    try
    {
        // Check explícito: existe la base?
        var creator = db.Database.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists())
        {
            logger.LogInformation("La base de datos para {DbContext} no existe. Creando...", typeof(TContext).Name);
            creator.Create();
        }

        // Esto crea tablas/aplica cambios según migraciones pendientes (y también crea la DB si hiciera falta)
        db.Database.Migrate();
    }
    catch (SqlException ex) when (
        db.Database.IsSqlServer()
        && (db.Database.GetConnectionString()?.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) ?? false))
    {
        // Caso típico: la PC no tiene instalado SQL Server Express LocalDB / instancia MSSQLLocalDB
        logger.LogError(ex, "No se pudo conectar a SQL Server LocalDB inicializando {DbContext}", typeof(TContext).Name);
        throw new InvalidOperationException(
            "No se pudo inicializar la base de datos porque SQL Server LocalDB no está disponible. " +
            "Instalá 'SQL Server Express LocalDB' en esta computadora (o cambiá la cadena de conexión en appsettings.json).",
            ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error inicializando la base de datos para {DbContext}", typeof(TContext).Name);
        throw;
    }
}

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
