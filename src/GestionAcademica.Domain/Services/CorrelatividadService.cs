using GestionAcademica.Domain.Entities;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Services;

/// <summary>
/// Lógica pura de correlatividades. Dada la lista de requisitos de una materia y el
/// conjunto de materias regularizadas/aprobadas del alumno, devuelve los requisitos
/// que todavía no se cumplen.
/// </summary>
public static class CorrelatividadService
{
    public static IReadOnlyList<Correlatividad> RequisitosIncumplidos(
        IEnumerable<Correlatividad> requisitos,
        IReadOnlySet<int> materiasRegularizadas,
        IReadOnlySet<int> materiasAprobadas)
    {
        var incumplidas = new List<Correlatividad>();

        foreach (var requisito in requisitos)
        {
            var cumple = requisito.TipoRequisito switch
            {
                // 'regularizada' se satisface con la materia regularizada o aprobada.
                TipoRequisito.Regularizada => materiasRegularizadas.Contains(requisito.IdMateriaRequisito)
                                              || materiasAprobadas.Contains(requisito.IdMateriaRequisito),
                // 'aprobada' exige nota de final aprobatoria.
                TipoRequisito.Aprobada => materiasAprobadas.Contains(requisito.IdMateriaRequisito),
                _ => false,
            };

            if (!cumple)
                incumplidas.Add(requisito);
        }

        return incumplidas;
    }
}
