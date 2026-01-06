using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data;

public class ClinicaDbContext : DbContext
{
    public ClinicaDbContext(DbContextOptions<ClinicaDbContext> options) : base(options)
    {
    }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<PlanObraSocial> PlanesObraSocial => Set<PlanObraSocial>();
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

        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.PlanObraSocial)
            .WithMany(p => p.Pacientes)
            .HasForeignKey(p => p.PlanObraSocialId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Turno>()
            .HasOne(t => t.ConsultaMedica)
            .WithOne()
            .HasForeignKey<Turno>(t => t.ConsultaMedicaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
