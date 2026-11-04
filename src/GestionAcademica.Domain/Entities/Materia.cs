using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `materias`.</summary>
public sealed class Materia
{
    public int IdMateria { get; set; }
    public int IdCarrera { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public short AnioEnCarrera { get; set; }
    public RegimenCursada RegimenCursada { get; set; }
    public int CargaHorariaTotal { get; set; }
    public bool Activa { get; set; } = true;

    public Carrera Carrera { get; set; } = null!;
    public ICollection<Correlatividad> Correlatividades { get; set; } = new List<Correlatividad>();
    public ICollection<Correlatividad> CorrelatividadesRequisito { get; set; } = new List<Correlatividad>();
    public ICollection<AsignacionDocente> AsignacionesDocentes { get; set; } = new List<AsignacionDocente>();
    public ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
    public ICollection<Examen> Examenes { get; set; } = new List<Examen>();
}
