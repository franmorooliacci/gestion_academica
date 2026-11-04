using System.Windows;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels;
using GestionAcademica.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestionAcademica.Desktop;

public partial class App : Application
{
    private IHost? _host;
    private SessionService? _session;
    private bool _handlingExpiration;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeService.ApplySystemTheme();

        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        ConfigureServices(builder.Services, builder.Configuration);

        _host = builder.Build();
        await _host.StartAsync();

        _session = _host.Services.GetRequiredService<SessionService>();
        _session.SessionExpired += OnSessionExpired;

        try
        {
            if (!ShowLogin())
            {
                Shutdown();
                return;
            }

            ShowShell();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo iniciar la aplicación:\n\n{ex.Message}",
                "Gestión Académica",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private bool ShowLogin()
    {
        var login = _host!.Services.GetRequiredService<LoginWindow>();
        return login.ShowDialog() == true;
    }

    private void ShowShell()
    {
        var shell = _host!.Services.GetRequiredService<ShellWindow>();
        MainWindow = shell;
        shell.Show();
    }

    /// <summary>
    /// La sesión venció y el refresh ya no es posible: se cierra el shell y se
    /// vuelve al login sin reiniciar la aplicación.
    /// </summary>
    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // Se difiere al dispatcher: la expiración puede llegar desde la continuación
        // de una request y no conviene abrir un diálogo modal dentro de esa pila.
        _ = Dispatcher.BeginInvoke(new Action(HandleSessionExpired));
    }

    private void HandleSessionExpired()
    {
        if (_handlingExpiration || _host is null || Dispatcher.HasShutdownStarted)
            return;

        _handlingExpiration = true;

        try
        {
            var previous = MainWindow;
            previous?.Hide();

            if (ShowLogin())
            {
                ShowShell();
                previous?.Close();
            }
            else
            {
                previous?.Close();
                Shutdown();
            }
        }
        catch (Exception)
        {
            MessageBox.Show(
                "No se pudo restablecer la sesión. La aplicación se cerrará.",
                "Gestión Académica",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
        finally
        {
            _handlingExpiration = false;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_session is not null)
            _session.SessionExpired -= OnSessionExpired;

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5125/";
        var apiUri = new Uri(baseUrl, UriKind.Absolute);

        services.AddSingleton<SessionService>();
        services.AddTransient<AuthHeaderHandler>();
        services.AddTransient<TokenRefreshHandler>();
        services.AddTransient<ITokenRefreshService, TokenRefreshService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();

        services.AddHttpClient<IAuthApi, AuthApi>(client => client.BaseAddress = apiUri);

        AddAuthenticatedApi<IPersonasApi, PersonasApi>(services, apiUri);
        AddAuthenticatedApi<IRolesApi, RolesApi>(services, apiUri);
        AddAuthenticatedApi<ICarrerasApi, CarrerasApi>(services, apiUri);
        AddAuthenticatedApi<IMateriasApi, MateriasApi>(services, apiUri);
        AddAuthenticatedApi<IPeriodosApi, PeriodosApi>(services, apiUri);
        AddAuthenticatedApi<IComisionesApi, ComisionesApi>(services, apiUri);
        AddAuthenticatedApi<IAsignacionesDocentesApi, AsignacionesDocentesApi>(services, apiUri);
        AddAuthenticatedApi<IExamenesApi, ExamenesApi>(services, apiUri);
        AddAuthenticatedApi<IInscripcionesComisionApi, InscripcionesComisionApi>(services, apiUri);

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<PersonasViewModel>();
        services.AddTransient<CarrerasViewModel>();
        services.AddTransient<MateriasViewModel>();
        services.AddTransient<PeriodosViewModel>();
        services.AddTransient<ComisionesViewModel>();
        services.AddTransient<AsignacionesDocentesViewModel>();
        services.AddTransient<ExamenesViewModel>();
        services.AddTransient<InscripcionesViewModel>();
        services.AddTransient<ActasViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<ShellWindow>();
        services.AddTransient<PersonasPage>();
        services.AddTransient<CarrerasPage>();
        services.AddTransient<MateriasPage>();
        services.AddTransient<PeriodosPage>();
        services.AddTransient<ComisionesPage>();
        services.AddTransient<AsignacionesDocentesPage>();
        services.AddTransient<ExamenesPage>();
        services.AddTransient<InscripcionesPage>();
        services.AddTransient<ActasPage>();
    }

    private static void AddAuthenticatedApi<TClient, TImplementation>(IServiceCollection services, Uri apiUri)
        where TClient : class
        where TImplementation : class, TClient
    {
        services
            .AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = apiUri)
            .AddHttpMessageHandler<TokenRefreshHandler>()
            .AddHttpMessageHandler<AuthHeaderHandler>();
    }
}
