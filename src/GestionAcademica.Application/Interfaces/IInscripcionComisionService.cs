using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Interfaces;

/// <summary>
/// Operaciones transaccionales de inscripción a comisiones. La implementación
/// (Infrastructure) aplica bloqueo pesimista (SELECT ... FOR UPDATE) sobre la fila
/// de la comisión para serializar las altas/bajas concurrentes y evitar la sobreventa
/// de cupos.
/// </summary>
public interface IInscripcionComisionService
{
    /// <summary>Da de alta al alumno en la comisión devolviendo la inscripción creada (inscripto/en_espera).</summary>
    Task<InscripcionComision> InscribirAsync(int idInscripcionCarrera, int idComision, CancellationToken ct);

    /// <summary>Da de baja la inscripción y promueve al primer 'en_espera' dentro de la misma transacción.</summary>
    Task DarDeBajaAsync(int idInscripcionComision, CancellationToken ct);
}
