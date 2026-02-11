using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data;

public static class DbInitializer
{
    public static void SeedData(ClinicaDbContext context)
    {
        // 1. Especialidades
        if (!context.Especialidades.Any())
        {
            var especialidades = new List<string>
            {
                "Cardiología", "Pediatría", "Dermatología", "Ginecología", "Oftalmología",
                "Traumatología", "Neurología", "Psiquiatría", "Urología", "Endocrinología",
                "Gastroenterología", "Neumonología", "Nefrología", "Hematología", "Infectología",
                "Otorrinolaringología", "Reumatología", "Kinesiología", "Nutrición", "Fonoaudiología",
                "Obstetricia", "Oncología", "Cirugía General", "Medicina General", "Clínica Médica"
            };

            foreach (var nombre in especialidades)
            {
                context.Especialidades.Add(new Especialidad { Nombre = nombre });
            }
            context.SaveChanges();
        }

        // 2. Obras Sociales y Planes
        if (!context.ObrasSociales.Any())
        {
            for (int i = 1; i <= 25; i++)
            {
                var os = new ObraSocial { Nombre = $"Obra Social {i}", Codigo = $"OS{i:D3}" };
                os.Planes.Add(new PlanObraSocial { Nombre = $"Plan Oro {i}" });
                os.Planes.Add(new PlanObraSocial { Nombre = $"Plan Plata {i}" });
                context.ObrasSociales.Add(os);
            }
            context.SaveChanges();
        }

        // 3. Consultorios
        if (!context.Consultorios.Any())
        {
            for (int i = 1; i <= 10; i++)
            {
                context.Consultorios.Add(new Consultorio { Nombre = $"Consultorio {i}", Piso = $"Piso {(i % 3) + 1}" });
            }
            context.SaveChanges();
        }

        // 4. Medicos
        if (context.Medicos.Count() < 50)
        {
            var espIds = context.Especialidades.Select(e => e.EspecialidadId).ToList();
            var nombres = new[] { "Juan", "Maria", "Carlos", "Ana", "Luis", "Elena", "Pedro", "Lucia", "Jorge", "Sofia" };
            var apellidos = new[] { "Garcia", "Rodriguez", "Lopez", "Perez", "Gonzalez", "Martinez", "Sanchez", "Fernandez", "Gomez", "Diaz" };
            
            var random = new Random();
            for (int i = context.Medicos.Count() + 1; i <= 50; i++)
            {
                context.Medicos.Add(new Medico
                {
                    Nombre = nombres[random.Next(nombres.Length)],
                    Apellido = apellidos[random.Next(apellidos.Length)],
                    Matricula = $"M{random.Next(10000, 99999)}",
                    EspecialidadId = espIds[random.Next(espIds.Count)]
                });
            }
            context.SaveChanges();
        }

        // 5. Pacientes
        if (context.Pacientes.Count() < 50)
        {
            var planIds = context.PlanesObraSocial.Select(p => p.PlanObraSocialId).ToList();
            var nombres = new[] { "Ricardo", "Monica", "Hugo", "Rosa", "Fabian", "Silvia", "Daniel", "Patricia", "Gustavo", "Beatriz" };
            var apellidos = new[] { "Acosta", "Rojas", "Villalba", "Gimenez", "Cardozo", "Torres", "Ramirez", "Flores", "Benitez", "Castro" };
            
            var random = new Random();
            for (int i = context.Pacientes.Count() + 1; i <= 50; i++)
            {
                var osPlanId = planIds[random.Next(planIds.Count)];
                var plan = context.PlanesObraSocial.AsNoTracking().FirstOrDefault(p => p.PlanObraSocialId == osPlanId);

                context.Pacientes.Add(new Paciente
                {
                    Nombre = nombres[random.Next(nombres.Length)],
                    Apellido = apellidos[random.Next(apellidos.Length)],
                    TipoDocumento = "DNI",
                    NumeroDocumento = random.Next(10000000, 45000000).ToString(),
                    FechaNacimiento = DateTime.Today.AddYears(-random.Next(1, 90)).AddDays(random.Next(1, 365)),
                    Email = $"paciente{i}@ejemplo.com",
                    Telefono = $"11{random.Next(40000000, 60000000)}",
                    Direccion = $"Calle Falsa {random.Next(100, 9999)}",
                    ObraSocialId = plan?.ObraSocialId,
                    PlanObraSocialId = osPlanId,
                    NumeroHistoriaClinica = $"HC{random.Next(100000, 999999)}"
                });
            }
            context.SaveChanges();
        }

        // 6. Turnos y Consultas
        if (!context.Turnos.Any())
        {
            var medicoIds = context.Medicos.Select(m => m.MedicoId).ToList();
            var pacienteIds = context.Pacientes.Select(p => p.PacienteId).ToList();
            var consultorioIds = context.Consultorios.Select(c => c.ConsultorioId).ToList();
            
            var random = new Random();
            var baseDate = DateTime.Today.AddDays(-30);

            for (int i = 0; i < 50; i++)
            {
                var medicoId = medicoIds[random.Next(medicoIds.Count)];
                var pacienteId = pacienteIds[random.Next(pacienteIds.Count)];
                var fecha = baseDate.AddDays(random.Next(0, 60)).AddHours(random.Next(8, 20)).AddMinutes(random.Next(0, 59));

                var turno = new Turno
                {
                    MedicoId = medicoId,
                    PacienteId = pacienteId,
                    ConsultorioId = consultorioIds[random.Next(consultorioIds.Count)],
                    FechaHoraInicio = fecha,
                    FechaHoraFin = fecha.AddMinutes(20),
                    Estado = (EstadoTurno)random.Next(0, 5),
                    MotivoConsulta = "Consulta general de control"
                };

                // Si está atendido, crear consulta
                if (turno.Estado == EstadoTurno.Atendido)
                {
                    var consulta = new ConsultaMedica
                    {
                        PacienteId = pacienteId,
                        MedicoId = medicoId,
                        FechaConsulta = fecha,
                        MotivoConsulta = "Chequeo de rutina",
                        Anamnesis = "Paciente refiere sentirse bien.",
                        ExamenFisico = "Normal.",
                        Indicaciones = "Continuar con dieta saludable."
                    };

                    consulta.Diagnosticos.Add(new Diagnostico { Descripcion = "Control de salud de rutina", Codigo = "Z00.0" });
                    consulta.Recetas.Add(new Receta { Medicamento = "Paracetamol", Dosis = "1g", Frecuencia = "Cada 8hs", Duracion = "3 días" });

                    context.ConsultasMedicas.Add(consulta);
                    context.SaveChanges(); // Para obtener el ID

                    turno.ConsultaMedicaId = consulta.ConsultaMedicaId;
                }

                context.Turnos.Add(turno);
            }
            context.SaveChanges();
        }
    }
}
