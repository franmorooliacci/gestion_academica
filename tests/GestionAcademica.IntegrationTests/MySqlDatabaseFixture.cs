using System.Text.Json;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Infrastructure.Persistence;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace GestionAcademica.IntegrationTests;

/// <summary>
/// Crea una base MySQL aislada (`gestion_academica_test`), le aplica `database/schema.sql`
/// y deja los datos base (carrera, materia y período) para los tests de integración.
/// </summary>
/// <remarks>
/// La conexión se resuelve, en orden, desde:
/// 1. variable de entorno `GESTIONACADEMICA_TEST_CONNECTION`;
/// 2. `ConnectionStrings:DefaultConnection` de `src/GestionAcademica.Api/appsettings.Development.json`;
/// 3. `Server=localhost;Port=3306;Uid=root;Pwd=1234;`.
/// </remarks>
public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    public const string TestDatabaseName = "gestion_academica_test";

    public string ConnectionString { get; private set; } = null!;
    public DbContextOptions<GestionAcademicaDbContext> Options { get; private set; } = null!;

    public int IdCarrera { get; private set; }
    public int IdMateria { get; private set; }
    public int IdPeriodo { get; private set; }

    public async Task InitializeAsync()
    {
        var builder = new MySqlConnectionStringBuilder(ResolveServerConnection())
        {
            Database = string.Empty,
        };

        await using (var connection = new MySqlConnection(builder.ConnectionString))
        {
            try
            {
                await connection.OpenAsync();
            }
            catch (MySqlException ex)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar a MySQL para los tests de integración. " +
                    "Iniciá MySQL local o configurá la variable " +
                    "GESTIONACADEMICA_TEST_CONNECTION con una cadena de conexión válida.",
                    ex);
            }

            await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS `{TestDatabaseName}`;");
            await ExecuteAsync(connection,
                $"CREATE DATABASE `{TestDatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;");
        }

        builder.Database = TestDatabaseName;
        ConnectionString = builder.ConnectionString;

        await ApplySchemaAsync();

        Options = new DbContextOptionsBuilder<GestionAcademicaDbContext>()
            .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 0, 0)))
            .UseSnakeCaseNamingConvention()
            .Options;

        await SeedBaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public GestionAcademicaDbContext CreateContext() => new(Options);

    private async Task ApplySchemaAsync()
    {
        var script = await File.ReadAllTextAsync(FindSchemaFile());

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        foreach (var raw in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            // Quita las líneas de comentario `--` para que cada sentencia quede limpia.
            var sql = string.Join('\n', raw
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

            if (string.IsNullOrWhiteSpace(sql))
                continue;

            await ExecuteAsync(connection, sql);
        }
    }

    private async Task SeedBaseAsync()
    {
        await using var db = CreateContext();

        var carrera = new Carrera
        {
            Nombre = "Carrera de prueba de integración",
            Tipo = TipoCarrera.Grado,
            TituloOtorgado = "Título de prueba",
            DuracionAnios = 3,
            Activa = true,
        };
        var periodo = new Periodo
        {
            Anio = 2030,
            Tipo = TipoPeriodo.Anual,
            FechaInicio = new DateOnly(2030, 1, 1),
            FechaFin = new DateOnly(2030, 12, 31),
            InscripcionComisionesAbierta = true,
            InscripcionFinalesAbierta = true,
            Activo = true,
        };

        db.Carreras.Add(carrera);
        db.Periodos.Add(periodo);
        await db.SaveChangesAsync();

        var materia = new Materia
        {
            IdCarrera = carrera.IdCarrera,
            Codigo = "TST101",
            Nombre = "Materia de prueba de integración",
            AnioEnCarrera = 1,
            RegimenCursada = RegimenCursada.Cuatrimestral,
            CargaHorariaTotal = 64,
            Activa = true,
        };

        db.Materias.Add(materia);
        await db.SaveChangesAsync();

        IdCarrera = carrera.IdCarrera;
        IdMateria = materia.IdMateria;
        IdPeriodo = periodo.IdPeriodo;
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string FindSchemaFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "database", "schema.sql");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "No se encontró 'database/schema.sql'. Ejecutá los tests desde el repositorio.");
    }

    private static string ResolveServerConnection()
    {
        var configured = Environment.GetEnvironmentVariable("GESTIONACADEMICA_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "GestionAcademica.Api", "appsettings.Development.json");

            if (File.Exists(candidate))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(candidate));
                if (document.RootElement.TryGetProperty("ConnectionStrings", out var connections)
                    && connections.TryGetProperty("DefaultConnection", out var defaultConnection)
                    && defaultConnection.GetString() is { Length: > 0 } connectionString)
                {
                    return connectionString;
                }
            }

            directory = directory.Parent;
        }

        return "Server=localhost;Port=3306;Uid=root;Pwd=1234;";
    }
}

[CollectionDefinition(Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlDatabaseFixture>
{
    public const string Name = "mysql";
}
