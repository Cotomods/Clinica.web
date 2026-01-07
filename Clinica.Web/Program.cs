using System.Text.Json;
using Clinica.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar licencia QuestPDF (Community)
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseInMemoryDatabase("ClinicaDb"));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed de datos desde JSON (solo para desarrollo/pruebas)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClinicaDbContext>();
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

app.UseAuthorization();

// Ruta corta para el calendario de turnos: /Calendario
app.MapControllerRoute(
    name: "calendario",
    pattern: "Calendario/{medicoId?}",
    defaults: new { controller = "Calendario", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
