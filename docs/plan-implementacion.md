# Plan de implementación — GestionAcademica

Sistema de gestión académica. Backend unificado (ASP.NET Core Web API) + cliente de escritorio (WPF), 100% ecosistema .NET.

> Fuente de arquitectura: `docs/Arquitectura Seminario.docx` (Clean Architecture, API-first).
> El nombre del proyecto/solución es **GestionAcademica** (no `SistemaAcademico` ni `Academic`).

> **Estado (2026-11-04)**: **Fase 1 completa** (hitos 1.1 a 1.7). **Fase 2 completa** (hitos 2.1 a 2.4). El escritorio cubre toda la operación administrativa: personas/roles, carreras, materias + correlatividades, períodos (apertura de ventanas), comisiones (cupo/lista de espera), asignaciones docentes, mesas de examen, consulta de inscripciones y actas, y contexto de sesión con carrera activa y refresh de token. No quedan hitos pendientes de Fase 2; el próximo objetivo es la **Fase 3 (web)**, fuera del alcance actual.

---

## 1. Decisiones confirmadas

| Decisión | Elección |
|---|---|
| Versión .NET | **.NET 10 (LTS)** en toda la solución |
| Cliente de escritorio | **WPF + MVVM** (CommunityToolkit.Mvvm) |
| Base de datos | **MySQL Server** (Pomelo.EntityFrameworkCore.MySql) |
| Entorno local | MySQL Server nativo en Windows (`localhost:3306`), base `gestion_academica` |
| Persistencia | **Database First**: mapeo manual de entidades + DbContext desde `database/schema.sql` (sin migraciones) |
| ORM | **EF Core 9 + Pomelo 9.0.0** (Pomelo aún no soporta EF Core 10; corre sobre el runtime .NET 10) |
| Modificaciones de schema | Solo **campos/columnas** a tablas existentes. **Sin tablas nuevas** |
| Redis | **No implementado / futuro**; la concurrencia de cupos se resuelve con bloqueo pesimista `SELECT ... FOR UPDATE` |
| Auth | **JWT custom + BCrypt** |
| Certificados | **QuestPDF + QRCoder**: PDFs en carpeta local protegida, validación pública por QR |
| Tests | **xUnit**: unitarios de lógica pura + integración contra MySQL real en base aislada `gestion_academica_test` |
| Alcance backend (fase 1) | Todos los módulos (entregados por hitos) |
| Web (Blazor) | **Fase 3 futura** — fuera de este plan |

---

## 2. Reglas y restricciones del proyecto

1. **API-First**: ningún cliente (Desktop ni futura Web) accede directamente a MySQL. Todo pasa por la API. La lógica crítica (cupos, correlatividades, actas) vive solo en el backend.
2. **No agregar tablas** al schema; solo se permiten campos a tablas existentes.
3. **No hardcodear credenciales** en el repo (usar user-secrets / `appsettings.Development.json` gitignoreado).
4. **No duplicar lógica de negocio** en el cliente de escritorio.
5. **No escribir archivos** (en `/plan` ni en ningún lado) sin pedido explícito.
6. **Autorización por cliente**: el login recibe `client` (`desktop`/`web`). El escritorio emite tokens solo con el rol `administrativo`; la web, solo con `alumno`/`profesor`. Las policies validan el rol activo (`active_role`) y que ese rol esté presente en el token. El rol `administrativo` no se asigna desde la API: las cuentas administrativas se gestionan por seed/BD.

---

## 3. Cambios aplicados al `database/schema.sql`

1. **`correlatividades`** — nueva columna:
   `tipo_requisito ENUM('regularizada','aprobada') NOT NULL DEFAULT 'regularizada'`
   → distingue si el requisito se cumple con la materia *regularizada* (`condicion='regular'`) o *aprobada* (nota de final aprobatoria).

2. **`asignaciones_docentes`** — nueva columna:
   `id_comision INT NULL` (FK → `comisiones`, agregada por `ALTER TABLE` al final del archivo porque `comisiones` se define después).
   - `id_comision = NULL` → asignación a nivel materia (todas sus comisiones).
   - La clave única `uq_asignacion_docente` ahora incluye `id_comision`.

3. **`certificados`** — nueva columna:
   `codigo_validacion CHAR(32) NOT NULL` + `UNIQUE KEY uq_certificado_codigo`.
   → código público aleatorio (GUID sin guiones) impreso en el QR del PDF; permite validar la
   autenticidad del documento sin exponer el `id_certificado`.

4. **Convención de identificadores**: el schema usa prefijo `id_` (`id_persona`, `id_comision`, etc.). Se mantiene esa convención.

5. **Columnas con `ñ`** (`año`, `duracion_años`, `año_en_carrera`, `año_ultima_reinscripcion`): se conservan en BD, pero en C# se mapean con Fluent API (`HasColumnName("año")`) y propiedades `Anio`, `DuracionAnios`, etc.

6. **PKs en C#**: `IdRol`, `IdPersona`, `IdComision`, etc. (prefijo `id_`). EF no las autodetecta como PK, por lo que cada entidad define `HasKey(x => x.IdX)`.

---

## 4. Estructura de proyectos

```
GestionAcademica.slnx
src/
  GestionAcademica.Shared.Contracts/  # Enums + DTOs + roles compartidos API ↔ clientes (proyecto hoja)
    Constants/                        # AppRoles (administrativo, alumno, profesor)
    Enums/                            # ClientApp, Sexo, TipoCarrera, CargoDocente, Turno, ...
    DTOs/                             # LoginRequest/Response, UserDto, ...
    Serialization/                    # EnumMember helpers + converter JSON
  GestionAcademica.Domain/            # Entidades, excepciones y reglas de dominio puras
    Entities/                         # Rol, Persona, Carrera, Materia, Comision, ... (POCOs)
    Exceptions/                       # CupoExcedidoException, CorrelativaIncumplidaException, ...
    Services/                         # CorrelatividadService, CupoService (lógica pura, testable)
  GestionAcademica.Application/       # Casos de uso CQRS (MediatR) + validaciones
    Interfaces/                       # IApplicationDbContext, ICertificadoPdfGenerator, ICertificadoFileStorage, IJwtTokenGenerator, IPasswordHasher
    Models/                           # Modelos internos (CertificadoPdfModel, CertificadoArchivo)
    UseCases/                         # Auth, InscripcionesCursada, MesasExamen, GestionAcademica, Certificados
    Validators/                       # FluentValidation
  GestionAcademica.Infrastructure/    # Persistencia (DbContext + Fluent API) y servicios externos
    Persistence/                      # GestionAcademicaDbContext (HasKey, columnas, relaciones, query filters, enums)
    Security/                         # JwtTokenGenerator, PasswordHasher (BCrypt)
    FileStorage/                      # QuestPDF + guardado de certificados
    Seeding/                          # Datos de desarrollo (roles, usuario administrativo, carrera demo)
  GestionAcademica.Api/               # Backend único (ASP.NET Core Web API)
    Controllers/
    Middlewares/                      # Manejo global de errores (ProblemDetails)
    Program.cs
  GestionAcademica.Desktop/           # Cliente WPF administrativo (fase 2)
    Views/  ViewModels/  Services/    # Páginas + ViewModels MVVM + clientes tipados de la API
    ViewModels/Infrastructure/        # PageViewModelBase, EditorViewModelBase, OpcionSeleccion, EnumTextos
    Themes/                           # Tokens claros/oscuros y estilos compartidos
    Converters/                       # Converters de presentación (enums, visibilidad, roles)
tests/
  GestionAcademica.UnitTests/         # Correlatividades, cupos y JWT (sin BD)
  GestionAcademica.IntegrationTests/  # MySQL real: concurrencia de cupos (base gestion_academica_test)
```

### Paquetes clave

| Proyecto | Paquetes |
|---|---|
| Application | MediatR 12.4.1, FluentValidation 12.2.0 (+ DependencyInjectionExtensions), Microsoft.EntityFrameworkCore 9.0.0 |
| Infrastructure | Pomelo.EntityFrameworkCore.MySql 9.0.0, Microsoft.EntityFrameworkCore.Design 9.0.0, EFCore.NamingConventions 9.0.0, BCrypt.Net-Next 4.0.3, System.IdentityModel.Tokens.Jwt 8.23.0, MySqlConnector 2.6.2, QuestPDF 2026.9.1, QRCoder 1.8.1 |
| Api | Swashbuckle.AspNetCore 6.9.0, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.14 |
| Desktop | CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Http |
| Shared.Contracts | (solo enums/DTOs/serialización, sin dependencias externas) |
| UnitTests | xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.11.0 |
| IntegrationTests | xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.11.0, MySqlConnector 2.6.2 |

---

## 5. FASE 1 — Backend unificado

### Hito 1.1 — Base y persistencia
1. Aplicar `database/schema.sql` contra la base `gestion_academica` (ya creada, vacía).
2. Mapeo **manual** (Database First): entidades POCO en `Domain/Entities` + `GestionAcademicaDbContext` en `Infrastructure/Persistence` con `HasKey` explícito, snake_case, columnas con `ñ`, relaciones y soft-delete.
3. `Program.cs`: DI, Pomelo, CORS, logging, OpenAPI/Swagger.
4. Soft-delete con `HasQueryFilter` (`activo`/`activa`).
5. ENUMs serializados como **string** en JSON; en C# como `enum`.

**Entregable**: API levanta, se conecta a MySQL y expone Swagger. ✅ (implementado y verificado)

### Hito 1.2 — Autenticación
6. `POST /api/auth/login` (valida `username`/`password_hash` con BCrypt) ✅. `GET /api/auth/me` ✅.
7. `JwtTokenGenerator`: token con `sub`, `unique_name`, **roles del cliente**, rol activo, cliente (`desktop`/`web`) y carreras del alumno ✅. Cambio de rol activo sin re-login ✅ (`POST /api/auth/switch-role`). Carreras del alumno en el token ✅ (claim `carreras` con JSON `{ IdInscripcionCarrera, IdCarrera, NombreCarrera, Estado }`).
8. Seed de `roles` (`administrativo`, `alumno`, `profesor`) y usuario `administrativo` / `Administrativo1234!` (solo en Development) ✅.

**Entregable**: login funcionando con JWT, separación por cliente y verificación por rol activo. ✅ (verificado end-to-end contra MySQL)

### Hito 1.3 — Gestión Académica (ABM) ✅
9. CRUD de: personas/roles, carreras, materias, correlatividades, periodos (flags de ventana), comisiones y asignaciones docentes. ✅ (roles es catálogo de solo lectura; la asignación de roles se hace en personas vía `IdsRoles`, limitada a `alumno` y `profesor`)
10. Validaciones de unicidad (`dni`, `email`, `username`) y reglas de negocio. ✅

**Entregable**: el administrativo puede configurar estructura curricular y calendario. ✅ (implementado)

### Hito 1.4 — Inscripciones a cursada (núcleo crítico) ✅
11. Alta en comisión: verificación de ventana (`inscripcion_comisiones_abierta`), **correlatividades** (según `tipo_requisito`), y **cupos** con `SELECT ... FOR UPDATE` sobre `comisiones` (bloqueo pesimista). Resultado `inscripto` o `en_espera`. ✅
12. Baja (`estado='baja'`) → promoción automática del primer `en_espera` (por `fecha_inscripcion`) dentro de la misma transacción. ✅

**Entregable**: inscripción concurrente sin sobreventa de cupos. ✅ (implementado y verificado end-to-end contra MySQL: cupo, lista de espera, rechazo por lista llena y promoción tras la baja)

### Hito 1.5 — Mesas de examen ✅
13. Programación de mesas (`examenes`): CRUD con validación de materia/período y fecha dentro del período; inscripción a finales (ventana `inscripcion_finales_abierta`, control de carrera, no duplicidad y materia regularizada/aprobada); cierre de actas (`PATCH /api/examenes/{id}/acta`) con estado `presente`/`ausente` + nota `DECIMAL(4,2)` (presente exige 1–10; ausente sin nota). ✅

**Entregable**: el administrativo programa mesas, el alumno se inscribe a finales y el profesor cierra el acta. ✅ (implementado y verificado end-to-end contra MySQL: alta de mesa, inscripción, cierre con nota y rechazos por ventana cerrada, duplicado y nota inválida)

### Hito 1.6 — Certificados ✅
14. Generación PDF con QuestPDF (+ QR de validación), guardado en carpeta protegida local, registro de `ruta_archivo` en `certificados`, descarga. ✅
    - `POST /api/certificados` (alumno) genera `analitico`, `constancia_inscripcion` o `alumno_regular`, guarda el PDF en `Certificados:DirectorioBase` (fuera de `wwwroot`) y devuelve el DTO con el código de validación.
    - `GET /api/certificados` y `GET /api/certificados/{id}` (alumno, solo los propios; filtro opcional `idInscripcionCarrera`) listan los certificados emitidos.
    - `GET /api/certificados/{id}/archivo` descarga el PDF (alumno dueño; otro alumno → 403).
    - `GET /api/certificados/validar/{codigo}` es anónimo: es el destino del QR impreso en el documento.
    - El certificado de alumno regular exige al menos una cursada activa; si no, se rechaza con 400.

**Entregable**: el alumno solicita, descarga y valida sus certificados; el administrativo no accede al módulo. ✅ (implementado y verificado end-to-end contra MySQL: los tres tipos de PDF, archivo en disco, `ruta_archivo`, validación por código, descarga del dueño y rechazos 400/403/404)

### Hito 1.7 — Tests y endurecimiento ✅
15. UnitTests (correlativas, cupos) + IntegrationTests (concurrencia de cupos). ✅
    - `tests/GestionAcademica.UnitTests`: 26 tests de `CorrelatividadService`, `CupoService`, generación de JWT (roles, rol activo, cliente y carreras), alcance de roles por cliente y reglas de asignación de roles.
    - `tests/GestionAcademica.IntegrationTests`: 3 tests contra MySQL real que verifican que la inscripción concurrente no sobrevende el cupo, usa la lista de espera y promueve al primero ante una baja. La base de test es `gestion_academica_test` (se recrea y se le aplica `database/schema.sql` en cada corrida).
16. Middleware global de errores → `ProblemDetails` ✅ (implementado en `Api/Middlewares/ExceptionHandlingMiddleware`). Header `X-Client-Version`: no implementado (opcional).

**Entregable al cierre del hito**: `dotnet test GestionAcademica.slnx` ejecuta **29 tests** (26 unitarios + 3 de integración) sin fallos. ✅ (La suite creció a **35 tests** al sumar los tests de refresh de token del hito 2.4.)

> La conexión de los tests de integración se toma de `GESTIONACADEMICA_TEST_CONNECTION`, o de `src/GestionAcademica.Api/appsettings.Development.json`, o del default local `Server=localhost;Port=3306;Uid=root;Pwd=1234;`.

---

## 6. FASE 2 — App de escritorio WPF

### Hito 2.1 — Esqueleto ✅
- Shell WPF + MVVM (CommunityToolkit.Mvvm), DI (`Microsoft.Extensions.DependencyInjection`), typed `HttpClient` contra la API usando los DTOs de `Shared.Contracts`, pantalla de login (JWT en memoria). ✅
- `PageViewModelBase` / `EditorViewModelBase` (`ViewModels/Infrastructure/`) centralizan carga, errores de `ProblemDetails`, validación con `ObservableValidator` y cancelación por navegación. ✅
- Tokens visuales (`Themes/`), `ErrorTemplate`, estilos de grilla/formulario y XamlStyler. ✅

### Hito 2.2 — Operación administrativa ✅
- Pantallas de personas/roles, carreras/materias/correlatividades, periodos (apertura de ventanas), comisiones (cupo/lista de espera), asignaciones docentes y mesas de examen. Grillas densas + entrada por teclado. ✅
  - **Personas** (`PersonasPage`): alta/edición, búsqueda y asignación de roles `alumno`/`profesor`.
  - **Carreras** (`CarrerasPage`): ABM con tipo, título y duración.
  - **Materias** (`MateriasPage`): ABM por carrera y gestión de **correlatividades** (`regularizada`/`aprobada`) desde el editor lateral.
  - **Períodos** (`PeriodosPage`): calendario y apertura de ventanas de inscripción a cursadas y finales.
  - **Comisiones** (`ComisionesPage`): cupo máximo, lista de espera, turno, modalidad y filtros por materia/período.
  - **Asignaciones docentes** (`AsignacionesDocentesPage`): docente, materia, comisión opcional, cargo y vigencia.
  - **Mesas de examen** (`ExamenesPage`): materia, período, fecha/hora y aula.
- Todas las pantallas siguen el molde `PersonasPage`/`PersonasViewModel`: editor lateral, `InfoBar`, estado vacío, contador, `ProgressRing` y atajos `Enter`, `Delete`, `Ctrl+N`, `Ctrl+S`, `Esc`, `F5`.
- Navegación del shell actualizada (todos los módulos habilitados) y registro por DI.
- Verificación: build de la solución con **0 warnings / 0 errores**, 26 tests unitarios + 3 de integración, y prueba de humo del escritorio (login, navegación por las siete pantallas y apertura/cierre de cada editor).

### Hito 2.3 — Consulta de la operación ✅
- Consulta de inscripciones y actas. El cierre de actas y la carga de notas se realizan desde la web del profesor; la solicitud de certificados, desde la web del alumno.
  - **Inscripciones** (`InscripcionesPage`): consulta de solo lectura de las inscripciones a comisión (`GET /api/inscripciones-comision`) con filtros por texto (alumno/materia/comisión), materia, período y estado (`inscripto`/`en espera`/`baja`), condición de cursada y fecha de inscripción.
  - **Actas** (`ActasPage`): grilla de mesas de examen (`GET /api/examenes`) y, al seleccionar una, el acta correspondiente (`GET /api/examenes/{id}/inscripciones`) con alumno, estado (`inscripto`/`presente`/`ausente`), nota y resumen de presentes/ausentes/sin cerrar. Sin edición ni cierre de acta desde el escritorio.
- Ambas pantallas son de solo consulta: no exponen alta, edición ni borrado, y respetan el molde `PersonasPage`/`PersonasViewModel` (filtros arriba, contador, estado vacío, `ProgressRing`, `InfoBar` y atajos `F5`; en Actas además `Enter` para ver/recargar el acta y `Esc` para cerrarla).
- Clientes tipados nuevos/extendidos: `IInscripcionesComisionApi` y `IExamenesApi.GetInscripcionesAsync`.
- Verificación: build de todos los proyectos (API, escritorio y tests) con **0 warnings / 0 errores**, 26 tests unitarios + 3 de integración en verde y prueba de humo del escritorio (login y navegación a las pantallas nuevas).

### Hito 2.4 — Contexto de sesión ✅
- **Carrera activa**: selector en la barra de estado del shell (`ShellViewModel`) con “Todas las carreras” más las carreras activas (`GET /api/carreras`). La selección se guarda en `SessionService` y se comparte con las pantallas.
  - Materias usa la carrera activa como filtro por defecto y como carrera inicial al crear una materia.
  - Comisiones, exámenes, actas, asignaciones docentes e inscripciones a comisión filtran sus filas por la carrera de la materia; los editores de alta inicializan materia/carrera según el contexto.
- **Refresh de token**:
  - `POST /api/auth/refresh` (anónimo) acepta un JWT vigente o vencido, valida firma/emisor/audiencia sin exigir vigencia y revalida el perfil y el rol activo contra la base antes de emitir un token nuevo. Implementado con `IJwtTokenParser` / `JwtTokenParser` y `RefreshTokenCommand`.
  - `TokenRefreshHandler` renueva el token de forma proactiva cuando faltan ≤2 minutos y reintenta una vez la request ante un `401`. `TokenRefreshService` serializa los refresh concurrentes.
  - Si el refresh ya no es posible (`401`/`403`), `SessionService` invalida la sesión y el shell vuelve al login sin reiniciar la aplicación.
- **Manejo de errores de API**: se mantiene la traducción centralizada de `ProblemDetails` en `ApiException` → `InfoBar`; el `401` de sesión dispara el refresh y, si falla, el retorno al login.
- El escritorio sigue operando exclusivamente con el rol `administrativo`; el cambio de rol activo continúa siendo responsabilidad de la web (alumno/profesor).
- Verificación: build de la solución con **0 warnings / 0 errores**, **32 tests unitarios + 3 de integración** en verde (se agregaron tests de lectura de JWT vencido/válido y rechazo de tokens inválidos) y prueba end-to-end del refresh contra MySQL real.
- *(Opcional futuro: auto-update con Velopack.)*

---

## 7. FASE 3 — Web (futura, fuera de alcance actual)

- `GestionAcademica.Web` (Blazor WASM + MudBlazor) como portal de autogestión de alumnos y profesores. Reutiliza `Shared.Contracts` y la misma API.

---

## 8. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Race condition en cupos | `SELECT ... FOR UPDATE` (implementado y verificado con tests de concurrencia); Redis `DECR` queda como optimización futura |
| Semántica de correlatividades/docentes | Resuelto con `tipo_requisito` e `id_comision` (ver sección 3) |
| Desincronización de enums cliente/API | ENUMs como string en JSON + `Shared.Contracts` + (futuro) header `X-Client-Version` |
| Certificados falsificados o acceso a PDFs ajenos | Código de validación aleatorio y único impreso en el QR; descarga autenticada con control de titularidad (403); PDFs fuera de `wwwroot`, servidos solo por la API |
| Credenciales expuestas | user-secrets / `appsettings.Development.json` gitignoreado |

---

## 9. Comandos útiles

```powershell
# Build y tests
dotnet build GestionAcademica.slnx
dotnet test GestionAcademica.slnx

# Solo unitarios / solo integración (los integración requieren MySQL)
dotnet test tests/GestionAcademica.UnitTests
dotnet test tests/GestionAcademica.IntegrationTests

# Ejecutar API (perfil http -> http://localhost:5125, Swagger en /swagger)
dotnet run --project src/GestionAcademica.Api --launch-profile http

# Re-aplicar schema (desde cmd)
"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" --user=root --password=1234 --default-character-set=utf8mb4 gestion_academica < database\schema.sql
```

> `database/schema.sql` contiene `CREATE TABLE` y **no es idempotente**. Sobre una base existente,
> los cambios de columnas se aplican con `ALTER TABLE` (p. ej. `codigo_validacion` en `certificados`).

> La persistencia es **Database First con mapeo manual**: no se usa `dotnet ef scaffold` ni migraciones en el flujo normal. El comando de scaffold queda solo como referencia para inspeccionar el schema.

---

## 10. Configuración y credenciales (local)

- Base: `gestion_academica`, usuario `root`, contraseña `1234` (solo entorno local de desarrollo).
- Connection string (en `appsettings.Development.json`): `Server=localhost;Port=3306;Database=gestion_academica;Uid=root;Pwd=1234;`
- JWT: secret de desarrollo en `appsettings.Development.json` (`Jwt:Secret`); issuer/audience/expiración en `appsettings.json`.
- Certificados: `Certificados:DirectorioBase` (por defecto `storage/certificados`, relativa al directorio de la API) y `Certificados:UrlPublicaBase` (por defecto `http://localhost:5125`, usada en el QR). En producción usar una ruta absoluta protegida y la URL pública real.
- Tests de integración: base `gestion_academica_test` (se recrea y se le aplica `database/schema.sql` en cada corrida); conexión por `GESTIONACADEMICA_TEST_CONNECTION` o `appsettings.Development.json`.
- Usuario seed: `administrativo` / `Administrativo1234!` (rol `administrativo`).
