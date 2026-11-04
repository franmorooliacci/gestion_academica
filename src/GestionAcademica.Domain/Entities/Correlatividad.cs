using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `correlatividades`.</summary>
public sealed class Correlatividad
{
    public int IdCorrelatividad { get; set; }
    public int IdMateria { get; set; }
    public int IdMateriaRequisito { get; set; }
    public TipoRequisito TipoRequisito { get; set; } = TipoRequisito.Regularizada;

    public Materia Materia { get; set; } = null!;
    public Materia MateriaRequisito { get; set; } = null!;
}
