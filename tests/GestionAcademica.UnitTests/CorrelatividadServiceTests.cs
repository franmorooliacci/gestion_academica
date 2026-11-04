using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Services;
using GestionAcademica.Shared.Contracts.Enums;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class CorrelatividadServiceTests
{
    private static Correlatividad Requisito(int idMateria, int idRequisito, TipoRequisito tipo)
        => new()
        {
            IdMateria = idMateria,
            IdMateriaRequisito = idRequisito,
            TipoRequisito = tipo,
        };

    [Fact]
    public void Requisito_regularizada_se_cumple_con_materia_regularizada()
    {
        var requisitos = new[] { Requisito(10, 1, TipoRequisito.Regularizada) };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int> { 1 },
            new HashSet<int>());

        Assert.Empty(incumplidas);
    }

    [Fact]
    public void Requisito_regularizada_se_cumple_con_materia_aprobada()
    {
        var requisitos = new[] { Requisito(10, 1, TipoRequisito.Regularizada) };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int>(),
            new HashSet<int> { 1 });

        Assert.Empty(incumplidas);
    }

    [Fact]
    public void Requisito_regularizada_falla_si_no_esta_en_ningun_conjunto()
    {
        var requisitos = new[] { Requisito(10, 1, TipoRequisito.Regularizada) };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int> { 99 },
            new HashSet<int> { 98 });

        Assert.Single(incumplidas);
        Assert.Equal(1, incumplidas[0].IdMateriaRequisito);
    }

    [Fact]
    public void Requisito_aprobada_no_se_cumple_solo_con_regularizada()
    {
        var requisitos = new[] { Requisito(10, 1, TipoRequisito.Aprobada) };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int> { 1 },
            new HashSet<int>());

        Assert.Single(incumplidas);
    }

    [Fact]
    public void Requisito_aprobada_se_cumple_con_materia_aprobada()
    {
        var requisitos = new[] { Requisito(10, 1, TipoRequisito.Aprobada) };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int>(),
            new HashSet<int> { 1 });

        Assert.Empty(incumplidas);
    }

    [Fact]
    public void Devuelve_solo_los_requisitos_incumplidos_en_orden()
    {
        var requisitos = new[]
        {
            Requisito(10, 1, TipoRequisito.Regularizada), // cumple (regular)
            Requisito(10, 2, TipoRequisito.Aprobada),     // cumple (aprobada)
            Requisito(10, 3, TipoRequisito.Regularizada), // no cumple
            Requisito(10, 4, TipoRequisito.Aprobada),     // no cumple (solo regular)
        };

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            requisitos,
            new HashSet<int> { 1, 4 },
            new HashSet<int> { 2 });

        Assert.Equal(new[] { 3, 4 }, incumplidas.Select(c => c.IdMateriaRequisito));
    }

    [Fact]
    public void Sin_requisitos_devuelve_lista_vacia()
    {
        var incumplidas = CorrelatividadService.RequisitosIncumplidos(
            Array.Empty<Correlatividad>(),
            new HashSet<int>(),
            new HashSet<int>());

        Assert.Empty(incumplidas);
    }
}
