using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Operación de períodos: calendario y apertura de ventanas de inscripción.</summary>
public partial class PeriodosViewModel : PageViewModelBase
{
    private readonly IPeriodosApi _api;
    private readonly IConfirmationService _confirmation;
    private readonly List<PeriodoDto> _todas = [];

    public PeriodosViewModel(IPeriodosApi api, IConfirmationService confirmation)
    {
        _api = api;
        _confirmation = confirmation;
    }

    public ObservableCollection<PeriodoDto> Periodos { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _soloActivos = true;

    [ObservableProperty]
    private PeriodoDto? _selectedPeriodo;

    [ObservableProperty]
    private PeriodoEditorViewModel? _editor;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private bool _mostrarVacio;

    partial void OnSearchTextChanged(string value) => Rebuild();

    partial void OnSoloActivosChanged(bool value) => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new PeriodoEditorViewModel(null);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(PeriodoDto? periodo)
    {
        periodo ??= SelectedPeriodo;
        if (periodo is null)
            return;

        Editor = new PeriodoEditorViewModel(periodo);
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
        var editor = Editor;
        if (editor is null)
            return;

        var validationError = editor.Validate();
        if (validationError is not null)
        {
            SetError(validationError);
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            var periodo = editor.EsAlta
                ? await _api.CreateAsync(editor.ToCreateRequest(), Token)
                : await _api.UpdateAsync(editor.Id!.Value, editor.ToUpdateRequest(), Token);

            string? ventanasError = null;
            try
            {
                await _api.UpdateVentanasAsync(periodo.Id, editor.ToVentanasRequest(), Token);
            }
            catch (ApiException ex)
            {
                ventanasError = ex.Message;
            }
            catch (HttpRequestException)
            {
                ventanasError = "El período se guardó, pero no se pudieron actualizar las ventanas.";
            }

            IsEditorOpen = false;
            Editor = null;
            await LoadAsync(Token);

            if (ventanasError is not null)
                SetError(ventanasError);
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
        var periodo = SelectedPeriodo;
        if (periodo is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar período",
            $"¿Eliminar el período {EnumTextos.Periodo(periodo.Anio, periodo.Tipo)}?\n\nQuedará inactivo.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(periodo.Id, Token);
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
            var periodos = await _api.GetAllAsync(ct);

            _todas.Clear();
            _todas.AddRange(periodos);
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
        var selectedId = SelectedPeriodo?.Id;
        var term = SearchText.Trim();

        var visibles = _todas.Where(periodo =>
            (!SoloActivos || periodo.Activo) &&
            (term.Length == 0 ||
             periodo.Anio.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
             EnumTextos.TipoPeriodo(periodo.Tipo).Contains(term, StringComparison.OrdinalIgnoreCase)));

        Periodos.Clear();
        foreach (var periodo in visibles)
            Periodos.Add(periodo);

        SelectedPeriodo = Periodos.FirstOrDefault(periodo => periodo.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Periodos.Count;
        MostrarVacio = !IsBusy && Periodos.Count == 0;
    }
}
