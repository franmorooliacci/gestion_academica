using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `carreras`.</summary>
public sealed class Carrera
{
    public int IdCarrera { get; set; }
    public string Nombre { get; set; } = null!;
    public TipoCarrera Tipo { get; set; } = TipoCarrera.Grado;
    public string TituloOtorgado { get; set; } = null!;
    public short DuracionAnios { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<Materia> Materias { get; set; } = new List<Materia>();
    public ICollection<InscripcionCarrera> InscripcionesCarrera { get; set; } = new List<InscripcionCarrera>();
}
