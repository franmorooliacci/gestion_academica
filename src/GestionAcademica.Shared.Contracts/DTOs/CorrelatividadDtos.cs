using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record CorrelatividadDto(
    int Id,
    int IdMateria,
    int IdMateriaRequisito,
    string MateriaRequisitoNombre,
    TipoRequisito TipoRequisito);

public sealed record CrearCorrelatividadRequest(int IdMateriaRequisito, TipoRequisito TipoRequisito);
