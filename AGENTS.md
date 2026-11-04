# AGENTS.md — GestionAcademica

Guía para agentes de IA y desarrolladores que trabajen en este repositorio.

## Stack
- .NET 10 (LTS). Todo el ecosistema es .NET (la web Blazor es fase 3, aún no existe).
- Backend: ASP.NET Core Web API (ÚNICO punto de acceso a datos).
- Escritorio: WPF + MVVM (CommunityToolkit.Mvvm).
- ORM: EF Core 9 + Pomelo.EntityFrameworkCore.MySql 9.0.0. Pomelo todavía no publica soporte para EF Core 10; EF Core 9 corre sobre el runtime .NET 10. Modo **Database First**: la BD es la fuente de verdad, **sin migraciones Code First**.
- Base de datos: MySQL Server. Schema fuente de verdad: `database/schema.sql`.
- Auth: JWT custom + BCrypt. CQRS: MediatR. Validación: FluentValidation. PDFs: QuestPDF.
- Tests: xUnit. Unitarios de lógica pura + integración contra MySQL real en la base aislada `gestion_academica_test`.
- Autorización: JWT emitido por cliente (`desktop`/`web`) con roles acotados a ese cliente; las policies validan el rol activo (`active_role`) y el rol emitido.

## Regla de oro (API-First)
- Ningún cliente (Desktop ni futura Web) accede directamente a MySQL. Todo pasa por la API.
- La lógica crítica (cupos, correlatividades, actas) vive SOLO en el backend.

## Estructura
- `src/GestionAcademica.Shared.Contracts` → enums + DTOs + roles compartidos API ↔ clientes (proyecto hoja, sin dependencias).
- `src/GestionAcademica.Domain`           → entidades (POCOs), excepciones y reglas de dominio puras.
- `src/GestionAcademica.Application`      → casos de uso CQRS + validadores + interfaces.
- `src/GestionAcademica.Infrastructure`   → DbContext + configuraciones EF (Fluent API), JWT, BCrypt, QuestPDF, seeding.
- `src/GestionAcademica.Api`              → backend único (controllers, middlewares, `Program.cs`).
- `src/GestionAcademica.Desktop`          → cliente WPF (fase 2).
- `tests/`                                → unit + integration.
  - `tests/GestionAcademica.UnitTests`          → correlatividades, cupos, JWT y reglas de autorización (sin BD).
  - `tests/GestionAcademica.IntegrationTests`   → MySQL real (base `gestion_academica_test`).

## Convenciones
- Proyectos con prefijo `GestionAcademica.*` (NO `Academic.` ni `SistemaAcademico`).
- Identificadores de BD: prefijo `id_` (`id_persona`, `id_comision`, ...). En C# las PK/FK se llaman
  `IdRol`, `IdPersona`, `IdComision`, etc.; la convención snake_case las mapea a `id_*`.
- EF **no autodetecta** esas PK: cada entidad requiere `HasKey(x => x.IdX)` explícito
  (la convención de EF solo reconoce `Id` o `<Tipo>Id`).
- ENUMs: viven en `Shared.Contracts` como `enum` con `[EnumMember(Value = "...")]`; se serializan
  como string en JSON y como columna ENUM en BD. El valor `mañana` se expone como `Turno.Maniana`.
- ENUMs **nullable** (ej. `InscripcionComision.Condicion`): `ApplyEnumConversions` (en
  `GestionAcademicaDbContext`) les aplica `NullableEnumMemberValueConverter<T>`. Nunca dejarlos sin
  converter: quedarían mapeados a int y la lectura/filtrado de esa columna fallaría.
- Columnas con `ñ` (`año`, `duracion_años`, `año_en_carrera`, `año_ultima_reinscripcion`):
  mapear con Fluent API (`HasColumnName("año")`) y propiedades `Anio`/`DuracionAnios`.
  Nunca usar identificadores con ñ en C#.
- Soft delete: columnas `activo`/`activa` con `HasQueryFilter`.
- Roles: `administrativo` (escritorio), `alumno` y `profesor` (web). Los nombres viven en
  `AppRoles`; las policies validan el rol activo del token, no cualquier rol de la persona.
- En `UpdatePersona`, `IdsRoles = null` significa "no modificar los roles"; una lista los sincroniza.
  El rol `administrativo` no se asigna ni se retira desde la API (solo seed/BD).
- Cambios de schema = editar `database/schema.sql` → re-aplicar → ajustar el mapeo EF
  (entidades + configuraciones). NO usar migraciones Code First.
- Errores: excepciones de dominio → middleware global → ProblemDetails.
- Tests: los unitarios no tocan la BD; los de integración usan la base aislada `gestion_academica_test`
  (el fixture la recrea y aplica `database/schema.sql`). Nunca apuntar tests a `gestion_academica`.

## Prohibido
- NO agregar tablas nuevas al schema (solo se permiten campos a tablas existentes).
- NO hardcodear credenciales reales (usar user-secrets / `appsettings.Development.json`).
- NO duplicar lógica de negocio en el cliente de escritorio.
- NO escribir archivos (en `/plan` ni en ningún lado) sin pedido explícito del usuario.

## Comandos
- `dotnet build GestionAcademica.slnx`
- `dotnet test GestionAcademica.slnx` (26 unitarios + 3 de integración; los de integración requieren MySQL local y usan la base `gestion_academica_test`)
- `dotnet test tests/GestionAcademica.UnitTests`
- `dotnet test tests/GestionAcademica.IntegrationTests` (conexión: `GESTIONACADEMICA_TEST_CONNECTION` o `appsettings.Development.json`)
- `dotnet run --project src/GestionAcademica.Api --launch-profile http`
- Re-aplicar schema (desde `cmd`):
  `"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" --user=root --password=1234 --default-character-set=utf8mb4 gestion_academica < database\schema.sql`
- Scaffold de referencia (opcional, para inspeccionar el schema):
  `dotnet ef dbcontext scaffold "Server=localhost;Port=3306;Database=gestion_academica;Uid=root;Pwd=1234" Pomelo.EntityFrameworkCore.MySql -o Persistence -f`

## Datos de desarrollo (seed)
- Usuario administrativo: `administrativo` / `Administrativo1234!` (se crea al arrancar en Development).
- Roles: `administrativo`, `alumno`, `profesor`.
- El escritorio es exclusivo del rol `administrativo`; la web es el canal de `alumno` y `profesor`. El login recibe `client` (`desktop` o `web`) y el token solo incluye los roles habilitados para ese cliente.
