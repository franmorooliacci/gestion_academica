using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Opción de rol del formulario de persona.</summary>
public partial class RolOpcion(RolDto rol, bool seleccionado) : ObservableObject
{
    public RolDto Rol { get; } = rol;

    public string Nombre => Rol.Nombre;

    [ObservableProperty]
    private bool _seleccionado = seleccionado;
}

/// <summary>
/// Estado y validación del formulario de alta/edición. No conoce la API ni la vista:
/// solo produce los requests del contrato.
/// </summary>
public partial class PersonaEditorViewModel : EditorViewModelBase
{
    public PersonaEditorViewModel(PersonaDto? persona, IReadOnlyList<RolDto> roles)
    {
        EsAlta = persona is null;
        Id = persona?.Id;
        Titulo = EsAlta ? "Nueva persona" : "Editar persona";

        var opciones = roles
            .Select(rol => new RolOpcion(rol, persona?.Roles.Any(actual => actual.Id == rol.Id) ?? false))
            .ToList();

        // Al editar, preservar los roles existentes que no son asignables desde la grilla
        // (p. ej. administrativo) para no perderlos al guardar.
        if (persona is not null)
        {
            foreach (var rol in persona.Roles.Where(actual => roles.All(disponible => disponible.Id != actual.Id)))
                opciones.Add(new RolOpcion(rol, true));
        }

        Roles = new ObservableCollection<RolOpcion>(opciones);

        if (persona is not null)
        {
            Nombre = persona.Nombre;
            Apellido = persona.Apellido;
            Dni = persona.Dni;
            Sexo = persona.Sexo;
            FechaNacimiento = persona.FechaNacimiento.ToDateTime(TimeOnly.MinValue);
            Telefono = persona.Telefono;
            Domicilio = persona.Domicilio;
            Email = persona.Email;
            Username = persona.Username;
        }
        else
        {
            FechaNacimiento = DateTime.Today.AddYears(-18);
        }
    }

    public int? Id { get; }

    public ObservableCollection<RolOpcion> Roles { get; }

    public IReadOnlyList<Sexo> Sexos { get; } = Enum.GetValues<Sexo>();

    [ObservableProperty]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    private string _nombre = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    private string _apellido = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos.")]
    private string _dni = string.Empty;

    [ObservableProperty]
    private Sexo _sexo = Sexo.PrefieroNoDecir;

    [ObservableProperty]
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    private DateTime? _fechaNacimiento;

    [ObservableProperty]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    private string? _telefono;

    [ObservableProperty]
    [MaxLength(200, ErrorMessage = "El domicilio no puede superar los 200 caracteres.")]
    private string? _domicilio;

    [ObservableProperty]
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    private string _email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El usuario no puede superar los 50 caracteres.")]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    partial void OnNombreChanged(string value) => Revalidate(value, nameof(Nombre));

    partial void OnApellidoChanged(string value) => Revalidate(value, nameof(Apellido));

    partial void OnDniChanged(string value) => Revalidate(value, nameof(Dni));

    partial void OnFechaNacimientoChanged(DateTime? value) => Revalidate(value, nameof(FechaNacimiento));

    partial void OnTelefonoChanged(string? value) => Revalidate(value, nameof(Telefono));

    partial void OnDomicilioChanged(string? value) => Revalidate(value, nameof(Domicilio));

    partial void OnEmailChanged(string value) => Revalidate(value, nameof(Email));

    partial void OnUsernameChanged(string value) => Revalidate(value, nameof(Username));

    protected override string? ValidateAdicional()
    {
        if (!EsAlta)
            return null;

        if (string.IsNullOrWhiteSpace(Password))
            return "La contraseña es obligatoria en un alta.";

        return Password.Length < 8 ? "La contraseña debe tener al menos 8 caracteres." : null;
    }

    public CrearPersonaRequest ToCreateRequest() => new(
        Nombre.Trim(),
        Apellido.Trim(),
        Dni.Trim(),
        Sexo,
        DateOnly.FromDateTime(FechaNacimiento!.Value),
        NullIfEmpty(Telefono),
        NullIfEmpty(Domicilio),
        Email.Trim(),
        Username.Trim(),
        Password,
        SelectedRoleIds());

    public ActualizarPersonaRequest ToUpdateRequest() => new(
        Nombre.Trim(),
        Apellido.Trim(),
        Dni.Trim(),
        Sexo,
        DateOnly.FromDateTime(FechaNacimiento!.Value),
        NullIfEmpty(Telefono),
        NullIfEmpty(Domicilio),
        Email.Trim(),
        Username.Trim(),
        SelectedRoleIds());

    private IReadOnlyList<int> SelectedRoleIds()
        => Roles.Where(rol => rol.Seleccionado).Select(rol => rol.Rol.Id).ToArray();

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
