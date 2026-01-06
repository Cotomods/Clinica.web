using Clinica.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Clinica.Web.Services;

public static class HistoriaClinicaPdfService
{
    public static byte[] GenerarHistoriaClinicaPdf(Paciente paciente, IEnumerable<ConsultaMedica> consultas)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text("Historia Clínica").FontSize(20).Bold().FontColor(Colors.Blue.Medium);

                    col.Item().Text($"Paciente: {paciente.Apellido} {paciente.Nombre}");
                    if (!string.IsNullOrEmpty(paciente.NumeroDocumento))
                        col.Item().Text($"Documento: {paciente.TipoDocumento} {paciente.NumeroDocumento}");
                    if (!string.IsNullOrEmpty(paciente.NumeroHistoriaClinica))
                        col.Item().Text($"Nº HC: {paciente.NumeroHistoriaClinica}");

                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    foreach (var c in consultas.OrderBy(c => c.FechaConsulta))
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Fecha: ").SemiBold();
                            text.Span(c.FechaConsulta.ToString("dd/MM/yyyy HH:mm"));
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Médico: ").SemiBold();
                            text.Span($"{c.Medico.Apellido} {c.Medico.Nombre}");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Motivo: ").SemiBold();
                            text.Span(c.MotivoConsulta);
                        });

                        if (!string.IsNullOrWhiteSpace(c.Anamnesis))
                        {
                            col.Item().Text("Anamnesis:").SemiBold();
                            col.Item().Text(c.Anamnesis).FontSize(10);
                        }

                        if (!string.IsNullOrWhiteSpace(c.ExamenFisico))
                        {
                            col.Item().Text("Examen físico:").SemiBold();
                            col.Item().Text(c.ExamenFisico).FontSize(10);
                        }

                        if (!string.IsNullOrWhiteSpace(c.Indicaciones))
                        {
                            col.Item().Text("Indicaciones:").SemiBold();
                            col.Item().Text(c.Indicaciones).FontSize(10);
                        }

                        if (c.Diagnosticos != null && c.Diagnosticos.Any())
                        {
                            col.Item().Text("Diagnósticos:").SemiBold();
                            foreach (var d in c.Diagnosticos)
                            {
                                col.Item().Text(text =>
                                {
                                    if (!string.IsNullOrWhiteSpace(d.Codigo))
                                    {
                                        text.Span($"[{d.Codigo}] ").SemiBold().FontSize(10);
                                    }
                                    text.Span(d.Descripcion).FontSize(10);
                                });
                            }
                        }

                        if (c.Recetas != null && c.Recetas.Any())
                        {
                            col.Item().Text("Recetas:").SemiBold();
                            foreach (var r in c.Recetas)
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span(r.Medicamento).SemiBold().FontSize(10);
                                    if (!string.IsNullOrWhiteSpace(r.Dosis))
                                    {
                                        text.Span($" - Dosis: {r.Dosis}").FontSize(10);
                                    }
                                    if (!string.IsNullOrWhiteSpace(r.Frecuencia))
                                    {
                                        text.Span($" - Frecuencia: {r.Frecuencia}").FontSize(10);
                                    }
                                    if (!string.IsNullOrWhiteSpace(r.Duracion))
                                    {
                                        text.Span($" - Duración: {r.Duracion}").FontSize(10);
                                    }
                                });
                            }
                        }

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                    }
                });
            });
        });

        return document.GeneratePdf();
    }
}
