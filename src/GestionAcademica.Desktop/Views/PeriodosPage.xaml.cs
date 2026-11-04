using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestionAcademica.Desktop.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace GestionAcademica.Desktop.Views;

public partial class PeriodosPage : Page, INavigableView<PeriodosViewModel>
{
    private readonly PeriodosViewModel _viewModel;

    public PeriodosPage(PeriodosViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Expone el ViewModel para que WPF-UI dispare <c>INavigationAware</c>.</summary>
    public PeriodosViewModel ViewModel => _viewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        SearchBox.Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
        => _viewModel.CancelPendingWork();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var enGrilla = DataGridPeriodos.IsKeyboardFocusWithin;

        switch (e.Key)
        {
            case Key.N when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.NuevaCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.GuardarCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F5:
                _viewModel.RefrescarCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                _viewModel.CancelarEdicionCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter when enGrilla:
                _viewModel.EditarCommand.Execute(_viewModel.SelectedPeriodo);
                e.Handled = true;
                break;

            case Key.Delete when enGrilla:
                _viewModel.EliminarCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
