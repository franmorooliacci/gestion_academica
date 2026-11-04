using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.Models;
using GestionAcademica.Shared.Contracts.Enums;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionAcademica.Infrastructure.FileStorage;

/// <summary>
/// Genera los PDFs de certificados con QuestPDF e incrusta un QR que apunta al
/// endpoint público de validación. El código público es aleatorio (32 hex) y se
/// registra en la columna `codigo_validacion`.
/// </summary>
public sealed class CertificadoPdfGenerator : ICertificadoPdfGenerator
{
    public byte[] Generar(CertificadoPdfModel model)
    {
        var qr = GenerarQr(model.UrlValidacion);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Element(container => Encabezado(container, model));
                page.Content().PaddingVertical(16).Element(container => Contenido(container, model, qr));
                page.Footer().Element(container => Pie(container, model));
            });
        }).GeneratePdf();
    }

    private static byte[] GenerarQr(string texto)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(10);
    }

    // ---------- Encabezado / pie ----------

    private static void Encabezado(IContainer container, CertificadoPdfModel model)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("GESTIÓN ACADÉMICA").FontSize(17).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text("Sistema de Gestión Académica").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(170).AlignRight().Column(col =>
                {
                    col.Item().Text(TituloDocumento(model.Tipo)).FontSize(11).Bold()
                        .FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"Emitido: {model.FechaGeneracion:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(8).LineHorizontal(1.2f).LineColor(Colors.Blue.Darken3);
        });
    }

    private static void Pie(IContainer container, CertificadoPdfModel model)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Certificado N° {model.CodigoValidacion} · Generado el {model.FechaGeneracion:dd/MM/yyyy HH:mm}")
                    .FontSize(7.5f).FontColor(Colors.Grey.Darken2);

                row.ConstantItem(90).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Darken2));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    // ---------- Cuerpo ----------

    private static void Contenido(IContainer container, CertificadoPdfModel model, byte[] qr)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().AlignCenter().Text(TituloDocumento(model.Tipo))
                .FontSize(15).Bold().FontColor(Colors.Grey.Darken4);

            switch (model.Tipo)
            {
                case TipoCertificado.Analitico:
                    ComponerAnalitico(column, model);
                    break;
                case TipoCertificado.AlumnoRegular:
                    ComponerAlumnoRegular(column, model);
                    break;
                default:
                    ComponerConstanciaInscripcion(column, model);
                    break;
            }

            BloqueValidacion(column, model, qr);
        });
    }

    private static void ComponerConstanciaInscripcion(ColumnDescriptor column, CertificadoPdfModel model)
    {
        column.Item().Text(text =>
        {
            text.Span("Se certifica que ");
            text.Span(model.NombreCompleto).Bold();
            text.Span($", DNI {model.Dni}, se encuentra inscripto/a en la carrera ");
            text.Span(model.CarreraNombre).Bold();
            text.Span($", dependiente de esta institución, con una duración de {model.DuracionAnios} años.");
        });

        column.Item().Text(
            "La presente constancia se emite a pedido del interesado/a y da cuenta de los datos " +
            "registrados en el sistema académico a la fecha de emisión.");

        BloqueDatos(column, model);
    }

    private static void ComponerAlumnoRegular(ColumnDescriptor column, CertificadoPdfModel model)
    {
        column.Item().Text(text =>
        {
            text.Span("Se certifica que ");
            text.Span(model.NombreCompleto).Bold();
            text.Span($", DNI {model.Dni}, es alumno/a regular de la carrera ");
            text.Span(model.CarreraNombre).Bold();
            text.Span(" y se encuentra cursando las siguientes materias:");
        });

        TablaMaterias(column, model.Materias, mostrarNota: false);
    }

    private static void ComponerAnalitico(ColumnDescriptor column, CertificadoPdfModel model)
    {
        column.Item().Text(text =>
        {
            text.Span("Se certifica que ");
            text.Span(model.NombreCompleto).Bold();
            text.Span($", DNI {model.Dni}, inscripto/a en la carrera ");
            text.Span(model.CarreraNombre).Bold();
            text.Span(", registra el siguiente historial académico:");
        });

        TablaMaterias(column, model.Materias, mostrarNota: true);

        BloqueDatos(column, model);

        var aprobadas = model.Materias.Count(m => m.Estado == EstadoMateriaCertificado.Aprobada);
        column.Item().Text(text =>
        {
            text.Span("Materias aprobadas: ");
            text.Span($"{aprobadas} de {model.Materias.Count}").Bold();
            text.Span(".");
        });
    }

    /// <summary>Datos de alumno/carrera/inscripción, en una grilla simple de dos columnas.</summary>
    private static void BloqueDatos(ColumnDescriptor column, CertificadoPdfModel model)
    {
        column.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(125);
                columns.RelativeColumn();
            });

            FilaDato(table, "Alumno/a", model.NombreCompleto);
            FilaDato(table, "DNI", model.Dni);
            if (!string.IsNullOrWhiteSpace(model.Email))
                FilaDato(table, "Email", model.Email!);
            FilaDato(table, "Carrera", model.CarreraNombre);
            FilaDato(table, "Título otorgado", model.TituloOtorgado);
            FilaDato(table, "Tipo de carrera", TipoCarreraLegible(model.CarreraTipo));
            FilaDato(table, "Fecha de inscripción", model.FechaInscripcion.ToString("dd/MM/yyyy"));
            FilaDato(table, "Estado", EstadoInscripcionLegible(model.EstadoInscripcion));

            if (model.AnioUltimaReinscripcion.HasValue)
                FilaDato(table, "Última reinscripción", model.AnioUltimaReinscripcion.Value.ToString());
        });
    }

    private static void FilaDato(TableDescriptor table, string etiqueta, string valor)
    {
        table.Cell().PaddingVertical(2).PaddingRight(8).Text(etiqueta).Bold().FontSize(9.5f)
            .FontColor(Colors.Grey.Darken2);
        table.Cell().PaddingVertical(2).Text(valor).FontSize(9.5f);
    }

    private static void TablaMaterias(ColumnDescriptor column, IReadOnlyList<CertificadoMateriaPdf> materias, bool mostrarNota)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(52);
                columns.RelativeColumn(3);
                columns.ConstantColumn(34);
                columns.ConstantColumn(58);
                if (mostrarNota)
                {
                    columns.ConstantColumn(38);
                    columns.ConstantColumn(58);
                }
            });

            table.Header(header =>
            {
                HeaderCell(header, "Código");
                HeaderCell(header, "Materia");
                HeaderCell(header, "Año");
                HeaderCell(header, "Condición");
                if (mostrarNota)
                {
                    HeaderCell(header, "Nota");
                    HeaderCell(header, "Aprobada");
                }
            });

            foreach (var materia in materias)
            {
                BodyCell(table, materia.Codigo, alignRight: false);
                BodyCell(table, materia.Nombre, alignRight: false);
                BodyCell(table, materia.AnioEnCarrera.ToString(), alignRight: true);
                BodyCell(table, EtiquetaEstado(materia.Estado), alignRight: false);

                if (mostrarNota)
                {
                    BodyCell(table, materia.Nota?.ToString("0.00") ?? "-", alignRight: true);
                    BodyCell(table, materia.FechaAprobacion?.ToString("dd/MM/yyyy") ?? "-", alignRight: false);
                }
            }
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string texto)
    {
        header.Cell().Background(Colors.Blue.Darken3).PaddingVertical(4).PaddingHorizontal(4)
            .Text(texto).Bold().FontSize(8.5f).FontColor(Colors.White);
    }

    private static void BodyCell(TableDescriptor table, string texto, bool alignRight)
    {
        var cell = table.Cell().BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(4);

        if (alignRight)
            cell.AlignRight().Text(texto).FontSize(8.5f);
        else
            cell.Text(texto).FontSize(8.5f);
    }

    private static void BloqueValidacion(ColumnDescriptor column, CertificadoPdfModel model, byte[] qr)
    {
        column.Item().PaddingTop(20).Row(row =>
        {
            row.ConstantItem(72).Height(72).Image(qr).FitArea();

            row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(col =>
            {
                col.Item().Text("Validación del documento").Bold().FontSize(9);
                col.Item().Text($"Código: {model.CodigoValidacion}").FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
                col.Item().Text($"Verifique su autenticidad en: {model.UrlValidacion}")
                    .FontSize(8).FontColor(Colors.Blue.Darken1);
            });
        });
    }

    // ---------- Etiquetas ----------

    private static string TituloDocumento(TipoCertificado tipo) => tipo switch
    {
        TipoCertificado.Analitico => "ANALÍTICO DE CALIFICACIONES",
        TipoCertificado.ConstanciaInscripcion => "CONSTANCIA DE INSCRIPCIÓN",
        TipoCertificado.AlumnoRegular => "CERTIFICADO DE ALUMNO REGULAR",
        _ => "CERTIFICADO",
    };

    private static string EtiquetaEstado(EstadoMateriaCertificado estado) => estado switch
    {
        EstadoMateriaCertificado.Aprobada => "Aprobada",
        EstadoMateriaCertificado.Regular => "Regular",
        EstadoMateriaCertificado.EnCurso => "En curso",
        _ => "Pendiente",
    };

    private static string EstadoInscripcionLegible(EstadoInscripcionCarrera estado) => estado switch
    {
        EstadoInscripcionCarrera.Activa => "Activa",
        EstadoInscripcionCarrera.Inactiva => "Inactiva",
        EstadoInscripcionCarrera.Graduado => "Graduado/a",
        _ => estado.ToString(),
    };

    private static string TipoCarreraLegible(TipoCarrera tipo) => tipo switch
    {
        TipoCarrera.Curso => "Curso",
        TipoCarrera.Pregrado => "Pregrado",
        TipoCarrera.Grado => "Grado",
        TipoCarrera.Postgrado => "Postgrado",
        _ => tipo.ToString(),
    };
}
