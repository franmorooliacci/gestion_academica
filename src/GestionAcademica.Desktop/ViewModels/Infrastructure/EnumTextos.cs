using Enums = GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels.Infrastructure;

/// <summary>Etiquetas legibles de los enums, compartidas por converters y ViewModels.</summary>
public static class EnumTextos
{
    public static string TipoCarrera(Enums.TipoCarrera valor) => valor switch
    {
        Enums.TipoCarrera.Curso => "Curso",
        Enums.TipoCarrera.Pregrado => "Pregrado",
        Enums.TipoCarrera.Grado => "Grado",
        Enums.TipoCarrera.Postgrado => "Postgrado",
        _ => valor.ToString(),
    };

    public static string Sexo(Enums.Sexo valor) => valor switch
    {
        Enums.Sexo.Femenino => "Femenino",
        Enums.Sexo.Masculino => "Masculino",
        Enums.Sexo.NoBinario => "No binario",
        Enums.Sexo.Otro => "Otro",
        Enums.Sexo.PrefieroNoDecir => "Prefiero no decir",
        _ => valor.ToString(),
    };

    public static string RegimenCursada(Enums.RegimenCursada valor) => valor switch
    {
        Enums.RegimenCursada.Cuatrimestral => "Cuatrimestral",
        Enums.RegimenCursada.Anual => "Anual",
        _ => valor.ToString(),
    };

    public static string TipoPeriodo(Enums.TipoPeriodo valor) => valor switch
    {
        Enums.TipoPeriodo.PrimerCuatrimestre => "1º cuatrimestre",
        Enums.TipoPeriodo.SegundoCuatrimestre => "2º cuatrimestre",
        Enums.TipoPeriodo.Anual => "Anual",
        _ => valor.ToString(),
    };

    public static string Turno(Enums.Turno valor) => valor switch
    {
        Enums.Turno.Maniana => "Mañana",
        Enums.Turno.Tarde => "Tarde",
        Enums.Turno.Noche => "Noche",
        _ => valor.ToString(),
    };

    public static string ModalidadDictado(Enums.ModalidadDictado valor) => valor switch
    {
        Enums.ModalidadDictado.Presencial => "Presencial",
        Enums.ModalidadDictado.Virtual => "Virtual",
        Enums.ModalidadDictado.Hibrida => "Híbrida",
        _ => valor.ToString(),
    };

    public static string CargoDocente(Enums.CargoDocente valor) => valor switch
    {
        Enums.CargoDocente.ProfesorTitular => "Profesor titular",
        Enums.CargoDocente.ProfesorAsociado => "Profesor asociado",
        Enums.CargoDocente.ProfesorAdjunto => "Profesor adjunto",
        Enums.CargoDocente.JefeTrabajosPracticos => "Jefe de trabajos prácticos",
        Enums.CargoDocente.AyudantePrimera => "Ayudante de primera",
        Enums.CargoDocente.AyudanteSegunda => "Ayudante de segunda",
        _ => valor.ToString(),
    };

    public static string TipoRequisito(Enums.TipoRequisito valor) => valor switch
    {
        Enums.TipoRequisito.Regularizada => "Regularizada",
        Enums.TipoRequisito.Aprobada => "Aprobada",
        _ => valor.ToString(),
    };

    public static string EstadoInscripcionComision(Enums.EstadoInscripcionComision valor) => valor switch
    {
        Enums.EstadoInscripcionComision.Inscripto => "Inscripto",
        Enums.EstadoInscripcionComision.EnEspera => "En espera",
        Enums.EstadoInscripcionComision.Baja => "Baja",
        _ => valor.ToString(),
    };

    public static string CondicionCursada(Enums.CondicionCursada valor) => valor switch
    {
        Enums.CondicionCursada.Regular => "Regular",
        Enums.CondicionCursada.Libre => "Libre",
        _ => valor.ToString(),
    };

    public static string EstadoInscripcionExamen(Enums.EstadoInscripcionExamen valor) => valor switch
    {
        Enums.EstadoInscripcionExamen.Inscripto => "Inscripto",
        Enums.EstadoInscripcionExamen.Presente => "Presente",
        Enums.EstadoInscripcionExamen.Ausente => "Ausente",
        _ => valor.ToString(),
    };

    public static string Periodo(short anio, Enums.TipoPeriodo tipo)
        => $"{anio} · {TipoPeriodo(tipo)}";
}
