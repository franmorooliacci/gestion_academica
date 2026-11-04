# Convenciones de UI — Escritorio WPF

Guía **normativa** del cliente de escritorio. Aplica a todo el código de `src/GestionAcademica.Desktop`.
Si una regla de acá choca con una preferencia personal, gana este documento.

- **MUST** = obligatorio. Un PR que lo incumple no se mergea.
- **SHOULD** = esperado; si te apartás, justificalo en el PR.
- **NEVER** = prohibido.

Referencia viva: `Views/PersonasPage.xaml` + `ViewModels/PersonasViewModel.cs` son la **pantalla molde**.
Toda pantalla nueva se copia estructuralmente de ahí.

---

## 1. Base tecnológica

| Pieza | Versión | Nota |
|---|---|---|
| WPF | `net10.0-windows` | `UseWPF=true` |
| WPF-UI | 4.3.x | Fluent / Windows 11. Único toolkit visual permitido |
| CommunityToolkit.Mvvm | 8.4.x | `[ObservableProperty]`, `[RelayCommand]`, `ObservableValidator` |
| Microsoft.Extensions.Hosting / Http | 10.0.x | DI + typed `HttpClient` |

- **NEVER** agregar otro toolkit visual (MahApps, MaterialDesign, HandyControl, etc.).
- **NEVER** reemplazar la plantilla de un control de WPF-UI con un `Style` propio sin `BasedOn`.
  Si hace falta otro look, se agrega un token o se pide un control nuevo; no se parchea el tema.
- **SHOULD** usar los controles de WPF-UI (`ui:Button`, `ui:TextBox`, `ui:DataGrid`, `ui:InfoBar`,
  `ui:ProgressRing`, `ui:SymbolIcon`) antes que los de WPF.

## 2. Tokens de diseño

Los valores visuales viven **solo** en `Themes/`:

| Archivo | Contenido |
|---|---|
| `Themes/Tokens.xaml` | colores del tema claro, tipografía, espaciado, tamaños, radios |
| `Themes/Tokens.Dark.xaml` | **solo** los overrides de color del tema oscuro |
| `Themes/Styles.Controls.xaml` | estilos de `TextBlock`/`Border` y converters compartidos |

Nomenclatura: `GA.<Categoría>.<Nombre>` (`GA.Brush.Layer`, `GA.Text.Title`, `GA.Size.Row`, `GA.Radius.Card`).

- **MUST** referenciar colores con `{DynamicResource GA.Brush.*}` (nunca `{StaticResource}`: rompe el cambio de tema).
- **MUST** referenciar tipografía con los estilos `GA.Text.*` (nunca `FontSize="13"`).
- **MUST** usar `GA.Size.Control` (32), `GA.Size.ControlLarge` (40) y `GA.Size.Row` (32) en vez de números.
- **MUST** referenciar márgenes y paddings con tokens `Thickness`: `GA.Thickness.*` (uniforme) o
  `GA.Gap.*` (direccional). Nunca `Margin="0,12,0,4"` literal en una vista.
- **MUST** referenciar altos/anchos fijos de control e íconos con tokens `GA.Size.*`
  (`GA.Size.SearchWidth`, `GA.Size.Progress`, `GA.Size.Dot`, …).
- **NEVER** escribir literales de color (`#RRGGBB`, `Red`, `Black`) en una vista.
- **NEVER** inventar un color/tamaño/espaciado nuevo en una vista: se agrega el token en `Tokens.xaml`
  **y** en `Tokens.Dark.xaml` si es color.
- **MAY** que un estilo de `Styles.Controls.xaml` fije su propio `Margin`/`Padding` interno.

#### Excepciones permitidas (solo layout, nunca tokens visuales)

- **Anchos de columna de `DataGrid`** (píxeles fijos como `Width="110"` y proporcionales como
  `Width="1.4*"`): son layout de grilla, no un valor de diseño reutilizable.
- **Dimensiones de ventana** (`Width`/`Height` de `Window`): forman parte del chrome, no del
  sistema de tokens.

### Escala tipográfica (Fluent 2)

| Token | Tamaño / interlineado | Uso |
|---|---|---|
| `GA.Text.TitleLarge` | 40 / 52 SemiBold | portadas, login |
| `GA.Text.Title` | 28 / 36 SemiBold | título de pantalla (uno solo por vista) |
| `GA.Text.Subtitle` | 20 / 28 SemiBold | título de panel/sección |
| `GA.Text.Body` / `BodyStrong` | 14 / 20 | contenido |
| `GA.Text.Caption` | 12 / 16 | ayudas, contadores, pie |
| `GA.Text.Label` | 12 / 16 SemiBold | label de campo |
| `GA.Text.Error` | 12 / 16 | error de campo |

### Espaciado y radios

Escala de 4 px: `GA.Space.2/4/8/12/16/24/32` (Double) y `GA.Thickness.4/8/12/16/24` (uniforme).
Márgenes direccionales: `GA.Gap.*` (p. ej. `GA.Gap.Field` = `0,12,0,4`).
Radios: `GA.Radius.Control` (4) y `GA.Radius.Card` (8).

## 3. Shell y navegación

- **MUST** `ui:FluentWindow` + `ui:TitleBar` + `ui:NavigationView` con panel izquierdo
  (`PaneDisplayMode="Left"`, ancho `GA.Size.SidebarWidth`).
- **MUST** navegar por tipo de página: `NavigationViewItem.TargetPageType` + `NavigationView.SetServiceProvider`
  (las páginas se resuelven por DI). Ver `Views/ShellWindow.xaml`.
- **MUST** que la barra de estado inferior muestre el usuario de la sesión.
- **NEVER** abrir una pantalla de la operación en una ventana nueva: todo ocurre en el shell,
  salvo el login y los diálogos de confirmación.
- Los ítems de módulos no implementados se muestran `IsEnabled="False"` (no se ocultan).

## 4. Densidad y grillas

La app es de operación administrativa: **densa, teclado primero**.

- **MUST** `ui:DataGrid` con: `AutoGenerateColumns="False"`, `IsReadOnly="True"`, `RowHeight` =
  `GA.Size.Row`, `HeadersVisibility="Column"`, `GridLinesVisibility="Horizontal"`,
  `SelectionUnit="FullRow"`, `EnableRowVirtualization="True"`, `CanUserAddRows="False"`.
- **MUST** `ElementStyle="{StaticResource GA.Grid.Text}"` en las columnas de texto.
- **MUST** que la grilla tenga: filtros arriba, contador de resultados abajo, estado vacío explícito
  y `ui:ProgressRing` superpuesto mientras carga.
- **MUST** que toda acción de la grilla exista también como atajo de teclado.
- **SHOULD** columnas en este orden: identificador corto → nombre → datos de contacto → estado → metadatos (roles, etc.).
- **MAY** los anchos de columna (fijos y proporcionales) sean literales: es la excepción de layout permitida (ver §2).
- **NEVER** usar colores de fila para comunicar estado; usar texto + indicador (punto/badge).
- **NEVER** poner acciones destructivas como botón suelto en la grilla; van en el editor o en menú contextual.

## 5. Formularios

- **MUST** panel lateral (`GA.Size.EditorWidth`, 360 px) con `GA.Card`, no modal.
- **MUST** label arriba (`GA.Text.Label`) y control a 32 px de alto.
- **MUST** validar con `ObservableValidator` + DataAnnotations y mostrar el resumen del error en el
  `ui:InfoBar` de la pantalla.
- **MUST** mostrar el error por campo con `Validation.ErrorTemplate="{StaticResource GA.Validation.ErrorTemplate}"`
  en cada control validable, y enlazar con `ValidatesOnNotifyDataErrors=True`. La revalidación en vivo
  se dispara recién después del primer intento de guardado.
- **MUST** que el editor tenga botones `Cancelar` (secundario) y `Guardar` (`Appearance="Primary"`) abajo a la derecha.
- **MUST** deshabilitar/cerrar el editor al guardar con éxito y recargar la grilla.
- **NEVER** guardar desde un `MessageBox`.
- **NEVER** poner lógica de negocio en el ViewModel del formulario: solo arma el request del contrato.

## 6. Feedback y errores

- **MUST** un único punto de traducción de errores de API: `Services/ApiException` sobre `ProblemDetails`
  (`detail`, `title`, `errors`). El mensaje que se muestra ya viene listo.
- **MUST** mostrar errores en el `ui:InfoBar` de la pantalla (`Severity="Error"`), no en un popup.
- **MUST** que el estado de carga se vea: `IsBusy` + `ui:ProgressRing`.
- **MUST** que el estado vacío explique qué hacer ("Sin personas para mostrar — ajustá la búsqueda o creá una nueva").
- **NEVER** mostrar `ex.ToString()`, stack traces ni códigos HTTP crudos al usuario.
- **NEVER** usar `MessageBox` salvo para confirmar una acción destructiva, y solo vía `IConfirmationService`.

## 7. Teclado y accesibilidad

- **MUST** estos atajos en toda pantalla de operación:

| Tecla | Acción |
|---|---|
| `Enter` | editar la fila seleccionada |
| `Delete` | eliminar la fila seleccionada (con confirmación) |
| `Ctrl+N` | nueva entidad |
| `Ctrl+S` | guardar el editor abierto |
| `Esc` | cancelar/cerrar el editor |
| `F5` | refrescar |

- **MUST** que el foco inicial caiga en el buscador al entrar a la pantalla.
- **MUST** orden de tabulación lógico (izquierda→derecha, arriba→abajo) y foco visible.
- **SHOULD** `AutomationProperties.Name` en iconos y botones sin texto.
- **SHOULD** probar la pantalla con `Tab` + `Enter` sin tocar el mouse antes de darla por terminada.

## 8. Estructura MVVM

```
GestionAcademica.Desktop/
  App.xaml(.cs)              # DI, tema, arranque (login → shell)
  Themes/                    # tokens y estilos
  Views/                     # XxxWindow / XxxPage (code-behind vacío salvo atajos y foco)
  ViewModels/                # XxxViewModel
  Services/                  # API clients, sesión, tema, confirmación
  Converters/                # IValueConverter
```

- **MUST** `XxxView` ↔ `XxxViewModel` (mismo nombre base).
- **MUST** que las pantallas hereden de `PageViewModelBase` y los formularios de `EditorViewModelBase`
  (`ViewModels/Infrastructure/`): el estado de carga/error/editor, el ciclo de vida de navegación y la
  validación común ya viven ahí. La pantalla solo implementa `LoadAsync` (y `InitializeCoreAsync` si
  necesita precargar catálogos).
- **MUST** que el ViewModel no referencie `System.Windows` ni `System.Windows.Controls`.
  Excepciones: `Dispatcher` si es imprescindible y `INavigationAware` (abstracción de navegación).
- **MUST** que el ViewModel dependa de interfaces (`IPersonasApi`), nunca de `HttpClient`.
- **MUST** que los DTOs vengan de `Shared.Contracts`; el escritorio **no** define modelos de API.
- **MUST** comandos async con `[RelayCommand]` y `CancellationToken` en las llamadas HTTP.
- **MUST** que la página implemente `INavigableView<TViewModel>` (expone `ViewModel`) y que el ViewModel
  implemente `INavigationAware`: inicializar en `OnNavigatedToAsync`, cancelar un `CancellationTokenSource`
  propio de la pantalla en `OnNavigatedFromAsync` y usar ese token en **todas** las llamadas HTTP.
  La página también debe cancelar en `Unloaded` como red de seguridad.
- **MUST** que el code-behind solo contenga: `InitializeComponent`, foco inicial, atajos a comandos y
  la cancelación de ciclo de vida.
- **NEVER** duplicar lógica de negocio (cupos, correlatividades, actas): vive en la API.

## 9. Sesión y API

- **MUST** que el JWT viva solo en memoria (`SessionService`). **NEVER** persistirlo en disco.
- **MUST** que el `Authorization: Bearer` lo agregue `AuthHeaderHandler`.
- **MUST** que la base URL venga de `appsettings.json` (`Api:BaseUrl`), no hardcodeada.
- **MUST** serializar con `ApiJson.Options` (enums como string, camelCase).
- **MUST** que un `401` lleve de vuelta al login. El escritorio intenta renovar el JWT
  (`TokenRefreshHandler` + `POST /api/auth/refresh`) y, si el refresh falla, cierra el shell
  y vuelve al login.
- **MUST** que la carrera activa del escritorio viva en `SessionService` y se seleccione desde el
  shell; las pantallas curriculares la usan como filtro por defecto.
- **NEVER** llamar a MySQL directo ni compartir el `DbContext` de la API.

## 10. Checklist antes de dar una pantalla por terminada

- [ ] No hay colores, tamaños ni márgenes literales en el XAML de la vista (salvo las excepciones de layout de §2: anchos de columna y dimensiones de ventana).
- [ ] Título único con `GA.Text.Title`; acciones primarias con `Appearance="Primary"`.
- [ ] Grilla con contador, estado vacío, progreso y atajos de teclado.
- [ ] Errores de API en `InfoBar`; ninguna excepción cruda visible.
- [ ] ViewModel sin `System.Windows`, sin `HttpClient` y sin lógica de negocio.
- [ ] `dotnet build GestionAcademica.slnx` sin warnings nuevos.
- [ ] La pantalla se opera completa con teclado.

## 11. Deuda conocida y pendientes

- Enforcement activo en el proyecto Desktop: `.editorconfig` raíz, `TreatWarningsAsErrors` y
  `EnforceCodeStyleInBuild`. Formato XAML: `dotnet tool restore` y
  `dotnet xstyler -r -d src/GestionAcademica.Desktop -c .xamlstyler.json`.
- `switch-role` (cambio de rol activo): aplica a la web (fase 3); el escritorio opera siempre
  como `administrativo`.
- Tema oscuro: `ThemeService.Apply(true)` ya funciona; falta el toggle en la UI.
