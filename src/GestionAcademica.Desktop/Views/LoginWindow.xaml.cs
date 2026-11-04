using System.Windows;
using GestionAcademica.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace GestionAcademica.Desktop.Views;

public partial class LoginWindow : FluentWindow
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e) => DialogResult = true;

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;
        base.OnClosed(e);
    }
}
