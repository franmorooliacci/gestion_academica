using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Domain.Services;
using GestionAcademica.Shared.Contracts.Enums;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class CupoServiceTests
{
    [Fact]
    public void Sin_cupo_configurado_ingresa_directo()
    {
        var estado = CupoService.DeterminarEstado(null, null, 500, 0, "C1");
        Assert.Equal(EstadoInscripcionComision.Inscripto, estado);
    }

    [Fact]
    public void Con_lugar_disponible_ingresa_como_inscripto()
    {
        var estado = CupoService.DeterminarEstado(10, 5, 9, 0, "C1");
        Assert.Equal(EstadoInscripcionComision.Inscripto, estado);
    }

    [Fact]
    public void Cupo_lleno_con_lugar_en_espera_ingresa_en_espera()
    {
        var estado = CupoService.DeterminarEstado(10, 5, 10, 4, "C1");
        Assert.Equal(EstadoInscripcionComision.EnEspera, estado);
    }

    [Fact]
    public void Cupo_lleno_sin_lista_de_espera_lanza_CupoExcedido()
    {
        var ex = Assert.Throws<CupoExcedidoException>(
            () => CupoService.DeterminarEstado(3, null, 3, 0, "C1"));

        Assert.Contains("C1", ex.Message);
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public void Cupo_lleno_con_lista_de_espera_completa_lanza_ListaEsperaLlena()
    {
        var ex = Assert.Throws<ListaEsperaLlenaException>(
            () => CupoService.DeterminarEstado(3, 2, 3, 2, "C2"));

        Assert.Contains("C2", ex.Message);
        Assert.Contains("2", ex.Message);
    }

    [Theory]
    [InlineData(0, 0, EstadoInscripcionComision.Inscripto)]
    [InlineData(4, 0, EstadoInscripcionComision.Inscripto)]
    [InlineData(5, 1, EstadoInscripcionComision.EnEspera)]
    public void Respeta_el_limite_exacto_del_cupo(int inscriptos, int enEspera, EstadoInscripcionComision esperado)
    {
        var estado = CupoService.DeterminarEstado(5, 3, inscriptos, enEspera, "C3");
        Assert.Equal(esperado, estado);
    }
}
