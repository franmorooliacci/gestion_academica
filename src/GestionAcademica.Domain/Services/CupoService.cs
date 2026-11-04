using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Services;

/// <summary>
/// Lógica pura de cupos de comisión. Determina si una inscripción ingresa como
/// 'inscripto' o 'en_espera', o si corresponde rechazarla.
/// </summary>
public static class CupoService
{
    public static EstadoInscripcionComision DeterminarEstado(
        int? cupoMaximo,
        int? listaEsperaMaxima,
        int inscriptosActuales,
        int enEsperaActuales,
        string codigoComision)
    {
        // Sin cupo configurado -> ingreso directo.
        if (cupoMaximo is null || inscriptosActuales < cupoMaximo.Value)
            return EstadoInscripcionComision.Inscripto;

        // Cupo lleno y sin lista de espera -> se rechaza.
        if (listaEsperaMaxima is null or 0)
            throw new CupoExcedidoException(codigoComision, cupoMaximo.Value);

        // Hay lista de espera: ingresa si todavía hay lugar.
        if (enEsperaActuales < listaEsperaMaxima.Value)
            return EstadoInscripcionComision.EnEspera;

        throw new ListaEsperaLlenaException(codigoComision, listaEsperaMaxima.Value);
    }
}
