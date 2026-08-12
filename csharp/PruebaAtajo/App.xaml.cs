using Microsoft.UI.Xaml;

namespace PruebaAtajo;

/// <summary>
/// Banco de pruebas del paso 1: solo atajo global y portapapeles. Sin
/// interfaz de verdad, a proposito — si se mezclara, un fallo del atajo
/// volveria a tener veinte sospechosos.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// El buzon vive tanto como la aplicacion, no como la ventana. Esa
    /// es justo la propiedad que se quiere comprobar.
    /// </summary>
    internal static Buzon? Buzon { get; private set; }

    internal static MainWindow? Ventana { get; private set; }

    public App()
    {
        InitializeComponent();

        // Tercer manejador global: los otros dos no ven lo que salta
        // dentro del arbol de XAML.
        UnhandledException += (_, a) =>
        {
            Registro.Fallo("Application.UnhandledException", a.Exception);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Ventana = new MainWindow();
        Ventana.Activate();

        // Se crea aqui, ya en el hilo de interfaz: RegisterHotKey falla
        // si el HWND lo creo otro hilo.
        Buzon = new Buzon();

        Ventana.Closed += (_, _) => Buzon?.Dispose();
    }
}
