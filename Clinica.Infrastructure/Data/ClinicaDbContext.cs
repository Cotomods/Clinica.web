using Clinica.Domain.Entities;
using Clinica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data;

public class ClinicaDbContext : IdentityDbContext<ApplicationUser>
{
    public ClinicaDbContext(DbContextOptions<ClinicaDbContext> options) : base(options)
    {
    }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<ConsultaMedica> ConsultasMedicas => Set<ConsultaMedica>();
    public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
    public DbSet<Receta> Recetas => Set<Receta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relaciones básicas y configuraciones mínimas
        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.ObraSocial)
            .WithMany(o => o.Pacientes)
            .HasForeignKey(p => p.ObraSocialId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Turno>()
            .HasOne(t => t.ConsultaMedica)
            .WithOne()
            .HasForeignKey<Turno>(t => t.ConsultaMedicaId)
            .OnDelete(DeleteBehavior.SetNull);

        // Evitar multiple cascade paths desde Medico hacia Turno (via ConsultaMedica y directamente)
        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Medico)
            .WithMany(m => m.Turnos)
            .HasForeignKey(t => t.MedicoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Especialidad es opcional para Medico
        modelBuilder.Entity<Medico>()
            .HasOne(m => m.Especialidad)
            .WithMany(e => e.Medicos)
            .HasForeignKey(m => m.EspecialidadId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índice único para evitar pacientes con mismo tipo+número de documento
        modelBuilder.Entity<Paciente>()
            .HasIndex(p => new { p.TipoDocumento, p.NumeroDocumento })
            .IsUnique()
            .HasFilter("[TipoDocumento] IS NOT NULL AND [NumeroDocumento] IS NOT NULL");
    }
}
