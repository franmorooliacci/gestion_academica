using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Infrastructure.Persistence;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionAcademica.IntegrationTests;

/// <summary>
/// Pruebas de concurrencia reales contra MySQL: la inscripción usa
/// `SELECT ... FOR UPDATE` sobre la comisión, por lo que nunca debe sobrevenderse
/// el cupo aunque lleguen altas simultáneas.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class InscripcionComisionConcurrencyTests
{
    private readonly MySqlDatabaseFixture _fixture;

    public InscripcionComisionConcurrencyTests(MySqlDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Concurrencia_no_sobrevende_el_cupo_y_usa_la_lista_de_espera()
    {
        const int cupo = 3;
        const int espera = 2;
        const int alumnos = 5;

        var (idComision, idsInscripcion) = await PrepararEscenarioAsync(cupo, espera, alumnos);

        var resultados = await InscribirConcurrenteAsync(idComision, idsInscripcion);

        Assert.All(resultados, r => Assert.Null(r.Error));
        Assert.Equal(cupo, resultados.Count(r => r.Inscripcion!.Estado == EstadoInscripcionComision.Inscripto));
        Assert.Equal(espera, resultados.Count(r => r.Inscripcion!.Estado == EstadoInscripcionComision.EnEspera));

        await using var db = _fixture.CreateContext();
        Assert.Equal(cupo, await db.InscripcionesComision.CountAsync(
            i => i.IdComision == idComision && i.Estado == EstadoInscripcionComision.Inscripto));
        Assert.Equal(espera, await db.InscripcionesComision.CountAsync(
            i => i.IdComision == idComision && i.Estado == EstadoInscripcionComision.EnEspera));
    }

    [Fact]
    public async Task Concurrencia_sin_lista_de_espera_rechaza_los_excedentes()
    {
        const int cupo = 1;
        const int alumnos = 3;

        var (idComision, idsInscripcion) = await PrepararEscenarioAsync(cupo, null, alumnos);

        var resultados = await InscribirConcurrenteAsync(idComision, idsInscripcion);

        Assert.Equal(cupo, resultados.Count(r => r.Inscripcion?.Estado == EstadoInscripcionComision.Inscripto));
        Assert.Equal(alumnos - cupo, resultados.Count(r => r.Error is CupoExcedidoException));
    }

    [Fact]
    public async Task Baja_promueve_al_primero_en_espera()
    {
        var (idComision, idsInscripcion) = await PrepararEscenarioAsync(cupo: 1, espera: 1, alumnos: 2);

        var primero = await InscribirAsync(idsInscripcion[0], idComision);
        var segundo = await InscribirAsync(idsInscripcion[1], idComision);

        Assert.Equal(EstadoInscripcionComision.Inscripto, primero.Inscripcion!.Estado);
        Assert.Equal(EstadoInscripcionComision.EnEspera, segundo.Inscripcion!.Estado);

        await using (var db = _fixture.CreateContext())
        {
            var service = new InscripcionComisionService(db);
            await service.DarDeBajaAsync(primero.Inscripcion.IdInscripcionComision, CancellationToken.None);
        }

        await using var verificacion = _fixture.CreateContext();
        var estados = await verificacion.InscripcionesComision.AsNoTracking()
            .Where(i => i.IdComision == idComision)
            .ToDictionaryAsync(i => i.IdInscripcionCarrera, i => i.Estado);

        Assert.Equal(EstadoInscripcionComision.Baja, estados[idsInscripcion[0]]);
        Assert.Equal(EstadoInscripcionComision.Inscripto, estados[idsInscripcion[1]]);
    }

    // ---------- Helpers ----------

    private sealed record ResultadoInscripcion(InscripcionComision? Inscripcion, Exception? Error);

    private async Task<(int IdComision, IReadOnlyList<int> IdsInscripcion)> PrepararEscenarioAsync(
        int cupo, int? espera, int alumnos)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];

        await using var db = _fixture.CreateContext();

        var comision = new Comision
        {
            IdMateria = _fixture.IdMateria,
            IdPeriodo = _fixture.IdPeriodo,
            Codigo = $"C{sufijo[..8]}",
            Turno = Turno.Noche,
            ModalidadDictado = ModalidadDictado.Presencial,
            CupoMaximo = cupo,
            ListaEsperaMaxima = espera,
            Activo = true,
        };
        db.Comisiones.Add(comision);
        await db.SaveChangesAsync();

        var personas = Enumerable.Range(0, alumnos)
            .Select(i => new Persona
            {
                Nombre = $"Alumno{i}",
                Apellido = "Integración",
                Dni = $"{sufijo[..6]}{i:D2}",
                Sexo = Sexo.PrefieroNoDecir,
                FechaNacimiento = new DateOnly(2000, 1, 1),
                Email = $"{sufijo}{i}@test.local",
                Username = $"u{sufijo}{i}",
                PasswordHash = "not-used",
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
            })
            .ToList();

        db.Personas.AddRange(personas);
        await db.SaveChangesAsync();

        var inscripciones = personas
            .Select(p => new InscripcionCarrera
            {
                IdPersona = p.IdPersona,
                IdCarrera = _fixture.IdCarrera,
                FechaInscripcion = DateTime.UtcNow,
                Estado = EstadoInscripcionCarrera.Activa,
            })
            .ToList();

        db.InscripcionesCarrera.AddRange(inscripciones);
        await db.SaveChangesAsync();

        return (comision.IdComision, inscripciones.Select(i => i.IdInscripcionCarrera).ToList());
    }

    private async Task<IReadOnlyList<ResultadoInscripcion>> InscribirConcurrenteAsync(
        int idComision, IReadOnlyList<int> idsInscripcion)
    {
        // Cada alta usa su propio DbContext/conexión: EF Core no es thread-safe.
        var tareas = idsInscripcion
            .Select(id => Task.Run(() => InscribirAsync(id, idComision)))
            .ToList();

        return await Task.WhenAll(tareas);
    }

    private async Task<ResultadoInscripcion> InscribirAsync(int idInscripcionCarrera, int idComision)
    {
        await using var db = _fixture.CreateContext();
        var service = new InscripcionComisionService(db);
        try
        {
            var inscripcion = await service.InscribirAsync(idInscripcionCarrera, idComision, CancellationToken.None);
            return new ResultadoInscripcion(inscripcion, null);
        }
        catch (Exception ex) when (ex is CupoExcedidoException or ListaEsperaLlenaException)
        {
            return new ResultadoInscripcion(null, ex);
        }
    }
}
