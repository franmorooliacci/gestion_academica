using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestionAcademica.Desktop.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace GestionAcademica.Desktop.Views;

public partial class InscripcionesPage : Page, INavigableView<InscripcionesViewModel>
{
    private readonly InscripcionesViewModel _viewModel;

    public InscripcionesPage(InscripcionesViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Expone el ViewModel para que WPF-UI dispare <c>INavigationAware</c>.</summary>
    public InscripcionesViewModel ViewModel => _viewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        SearchBox.Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
        => _viewModel.CancelPendingWork();

    /// <summary>Atajos de la grilla de solo consulta.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F5:
                _viewModel.RefrescarCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
