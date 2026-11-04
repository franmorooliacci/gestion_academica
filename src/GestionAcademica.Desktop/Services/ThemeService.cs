using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Tema de la aplicación: WPF-UI pinta los controles y acá se fusiona el override de tokens oscuros.
/// La única forma de cambiar de tema es llamar a <see cref="Apply"/>.
/// </summary>
public static class ThemeService
{
    private const string DarkTokensUri =
        "pack://application:,,,/GestionAcademica.Desktop;component/Themes/Tokens.Dark.xaml";

    public static bool IsDark { get; private set; }

    public static void ApplySystemTheme()
        => Apply(ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark);

    public static void Apply(bool dark)
    {
        IsDark = dark;

        ApplicationThemeManager.Apply(
            dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.Mica,
            updateAccent: true);

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var current = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source is not null &&
            dictionary.Source.OriginalString.EndsWith("Tokens.Dark.xaml", StringComparison.OrdinalIgnoreCase));

        if (current is not null)
            dictionaries.Remove(current);

        if (dark)
            dictionaries.Add(new ResourceDictionary { Source = new Uri(DarkTokensUri, UriKind.Absolute) });
    }
}
