using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestionAcademica.Desktop.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace GestionAcademica.Desktop.Views;

public partial class ActasPage : Page, INavigableView<ActasViewModel>
{
    private readonly ActasViewModel _viewModel;

    public ActasPage(ActasViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Expone el ViewModel para que WPF-UI dispare <c>INavigationAware</c>.</summary>
    public ActasViewModel ViewModel => _viewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        SearchBox.Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
        => _viewModel.CancelPendingWork();

    /// <summary>Atajos de la consulta: ver/recargar acta, cerrarla y refrescar.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var enMesas = DataGridMesas.IsKeyboardFocusWithin;

        switch (e.Key)
        {
            case Key.F5:
                _viewModel.RefrescarCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                _viewModel.CerrarActaCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter when enMesas:
                _viewModel.VerActaCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
