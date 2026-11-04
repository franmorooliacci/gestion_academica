using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `comisiones`.</summary>
public sealed class Comision
{
    public int IdComision { get; set; }
    public int IdMateria { get; set; }
    public int IdPeriodo { get; set; }
    public string Codigo { get; set; } = null!;
    public Turno Turno { get; set; }
    public ModalidadDictado ModalidadDictado { get; set; } = ModalidadDictado.Presencial;
    public int? CupoMaximo { get; set; }
    public int? ListaEsperaMaxima { get; set; }
    public bool Activo { get; set; } = true;

    public Materia Materia { get; set; } = null!;
    public Periodo Periodo { get; set; } = null!;
    public ICollection<AsignacionDocente> AsignacionesDocentes { get; set; } = new List<AsignacionDocente>();
    public ICollection<InscripcionComision> InscripcionesComision { get; set; } = new List<InscripcionComision>();
}
