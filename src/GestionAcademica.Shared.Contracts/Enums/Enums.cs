using System.Runtime.Serialization;

namespace GestionAcademica.Shared.Contracts.Enums;

/// <summary>
/// Enums del dominio, con los valores exactos que usa MySQL en sus columnas ENUM.
/// Los atributos [EnumMember] garantizan que JSON, EF Core y la base de datos
/// compartan los mismos literales (incluidos los que usan caracteres como 'ñ').
/// </summary>

/// <summary>Cliente desde el que se inicia sesión. Define los roles habilitados.</summary>
public enum ClientApp
{
    [EnumMember(Value = "desktop")] Desktop,
    [EnumMember(Value = "web")] Web,
}

public enum Sexo
{
    [EnumMember(Value = "femenino")] Femenino,
    [EnumMember(Value = "masculino")] Masculino,
    [EnumMember(Value = "no_binario")] NoBinario,
    [EnumMember(Value = "otro")] Otro,
    [EnumMember(Value = "prefiero_no_decir")] PrefieroNoDecir,
}

public enum TipoCarrera
{
    [EnumMember(Value = "curso")] Curso,
    [EnumMember(Value = "pregrado")] Pregrado,
    [EnumMember(Value = "grado")] Grado,
    [EnumMember(Value = "postgrado")] Postgrado,
}

public enum EstadoInscripcionCarrera
{
    [EnumMember(Value = "activa")] Activa,
    [EnumMember(Value = "inactiva")] Inactiva,
    [EnumMember(Value = "graduado")] Graduado,
}

public enum RegimenCursada
{
    [EnumMember(Value = "cuatrimestral")] Cuatrimestral,
    [EnumMember(Value = "anual")] Anual,
}

public enum CargoDocente
{
    [EnumMember(Value = "profesor_titular")] ProfesorTitular,
    [EnumMember(Value = "profesor_asociado")] ProfesorAsociado,
    [EnumMember(Value = "profesor_adjunto")] ProfesorAdjunto,
    [EnumMember(Value = "jefe_trabajos_practicos")] JefeTrabajosPracticos,
    [EnumMember(Value = "ayudante_primera")] AyudantePrimera,
    [EnumMember(Value = "ayudante_segunda")] AyudanteSegunda,
}

public enum TipoPeriodo
{
    [EnumMember(Value = "primer_cuatrimestre")] PrimerCuatrimestre,
    [EnumMember(Value = "segundo_cuatrimestre")] SegundoCuatrimestre,
    [EnumMember(Value = "anual")] Anual,
}

public enum Turno
{
    [EnumMember(Value = "mañana")] Maniana,
    [EnumMember(Value = "tarde")] Tarde,
    [EnumMember(Value = "noche")] Noche,
}

public enum ModalidadDictado
{
    [EnumMember(Value = "presencial")] Presencial,
    [EnumMember(Value = "virtual")] Virtual,
    [EnumMember(Value = "hibrida")] Hibrida,
}

public enum EstadoInscripcionComision
{
    [EnumMember(Value = "en_espera")] EnEspera,
    [EnumMember(Value = "inscripto")] Inscripto,
    [EnumMember(Value = "baja")] Baja,
}

public enum CondicionCursada
{
    [EnumMember(Value = "regular")] Regular,
    [EnumMember(Value = "libre")] Libre,
}

public enum EstadoInscripcionExamen
{
    [EnumMember(Value = "inscripto")] Inscripto,
    [EnumMember(Value = "presente")] Presente,
    [EnumMember(Value = "ausente")] Ausente,
}

public enum TipoCertificado
{
    [EnumMember(Value = "analitico")] Analitico,
    [EnumMember(Value = "constancia_inscripcion")] ConstanciaInscripcion,
    [EnumMember(Value = "alumno_regular")] AlumnoRegular,
}

public enum TipoRequisito
{
    [EnumMember(Value = "regularizada")] Regularizada,
    [EnumMember(Value = "aprobada")] Aprobada,
}
