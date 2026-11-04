using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Operación de carreras: grilla densa + editor lateral.</summary>
public partial class CarrerasViewModel : PageViewModelBase
{
    private readonly ICarrerasApi _api;
    private readonly IConfirmationService _confirmation;
    private readonly List<CarreraDto> _todas = [];

    public CarrerasViewModel(ICarrerasApi api, IConfirmationService confirmation)
    {
        _api = api;
        _confirmation = confirmation;
    }

    public ObservableCollection<CarreraDto> Carreras { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _soloActivas = true;

    [ObservableProperty]
    private CarreraDto? _selectedCarrera;

    [ObservableProperty]
    private CarreraEditorViewModel? _editor;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private bool _mostrarVacio;

    partial void OnSearchTextChanged(string value) => Rebuild();

    partial void OnSoloActivasChanged(bool value) => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new CarreraEditorViewModel(null);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(CarreraDto? carrera)
    {
        carrera ??= SelectedCarrera;
        if (carrera is null)
            return;

        Editor = new CarreraEditorViewModel(carrera);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        IsEditorOpen = false;
        Editor = null;
        SetError(null);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Editor is null)
            return;

        var validationError = Editor.Validate();
        if (validationError is not null)
        {
            SetError(validationError);
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            if (Editor.EsAlta)
                await _api.CreateAsync(Editor.ToCreateRequest(), Token);
            else
                await _api.UpdateAsync(Editor.Id!.Value, Editor.ToUpdateRequest(), Token);

            IsEditorOpen = false;
            Editor = null;
            await LoadAsync(Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudo conectar con la API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        var carrera = SelectedCarrera;
        if (carrera is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar carrera",
            $"¿Eliminar la carrera {carrera.Nombre}?\n\nQuedará inactiva.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(carrera.Id, Token);
            await LoadAsync(Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudo conectar con la API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        SetError(null);

        try
        {
            var carreras = await _api.GetAllAsync(ct);

            _todas.Clear();
            _todas.AddRange(carreras);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudo conectar con la API. Verificá que esté corriendo.");
        }
        finally
        {
            IsBusy = false;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        var selectedId = SelectedCarrera?.Id;
        var term = SearchText.Trim();

        var visibles = _todas.Where(carrera =>
            (!SoloActivas || carrera.Activa) &&
            (term.Length == 0 ||
             carrera.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             carrera.TituloOtorgado.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Carreras.Clear();
        foreach (var carrera in visibles)
            Carreras.Add(carrera);

        SelectedCarrera = Carreras.FirstOrDefault(carrera => carrera.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Carreras.Count;
        MostrarVacio = !IsBusy && Carreras.Count == 0;
    }
}
