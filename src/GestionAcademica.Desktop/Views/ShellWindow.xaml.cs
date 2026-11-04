using System.Windows;
using GestionAcademica.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace GestionAcademica.Desktop.Views;

public partial class ShellWindow : FluentWindow
{
    private readonly ShellViewModel _viewModel;

    public ShellWindow(IServiceProvider services, ShellViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        // Las páginas se resuelven por DI al navegar.
        RootNavigation.SetServiceProvider(services);

        ContentRendered += OnContentRendered;
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;

        RootNavigation.Navigate(typeof(PersonasPage));
        await _viewModel.InitializeAsync(CancellationToken.None);
    }
}
