using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PruebaAtajo;

/// <summary>
/// Los dos contadores. La prueba se pasa si 100 pulsaciones dan 100, y
/// si sigue contando con la ventana oculta y despues de horas
/// residente.
/// </summary>
public sealed partial class MainPage : Page
{
    int _atajos;
    int _copias;

    // El hilo de interfaz, apuntado al arrancar. Sirve para responder
    // la pregunta de fondo: si los mensajes de la ventana solo-mensajes
    // llegan por la bomba de WinUI, tienen que llegar en este hilo.
    readonly int _hiloInterfaz = Environment.CurrentManagedThreadId;

    public MainPage()
    {
        InitializeComponent();
        Loaded += AlCargar;
    }

    void AlCargar(object remitente, RoutedEventArgs args)
    {
        RutaLog.Text = Registro.Ruta;

        var buzon = App.Buzon;

        if (buzon is null)
        {
            Estado.Text = "El buzon no llego a crearse. Mira el log.";
            return;
        }

        Estado.Text = buzon.Problema
                      ?? $"Buzon en marcha (hwnd 0x{buzon.Handle:X}), "
                       + $"hilo de interfaz {_hiloInterfaz}. "
                       + "Pulsa Ctrl+Shift+V desde cualquier aplicacion.";

        buzon.Atajo += AlAtajo;
        buzon.Portapapeles += AlPortapapeles;
    }

    void AlAtajo()
    {
        int hilo = Environment.CurrentManagedThreadId;
        Registro.Anotar($"WM_HOTKEY numero {_atajos + 1}");

        Apuntar(() =>
        {
            _atajos++;
            ContadorAtajo.Text = _atajos.ToString();
            Ultimo.Text = Describir("atajo", hilo);
        });
    }

    void AlPortapapeles()
    {
        int hilo = Environment.CurrentManagedThreadId;
        Registro.Anotar($"WM_CLIPBOARDUPDATE numero {_copias + 1}");

        Apuntar(() =>
        {
            _copias++;
            ContadorPortapapeles.Text = _copias.ToString();
            Ultimo.Text = Describir("copia", hilo);
        });
    }

    string Describir(string que, int hilo) =>
        $"Ultimo {que}: {DateTime.Now:HH:mm:ss.fff}, entregado en el "
        + $"hilo {hilo}"
        + (hilo == _hiloInterfaz
            ? " (el de interfaz, que es lo esperado)."
            : " — NO es el de interfaz. Anotalo, importa.");

    /// <summary>
    /// Actualiza la interfaz por la cola del despachador. Si el mensaje
    /// llegase en otro hilo, tocar los controles a pelo tumbaria la
    /// aplicacion y se perderia justo el dato que se quiere medir.
    /// </summary>
    void Apuntar(Action trabajo)
    {
        if (!DispatcherQueue.TryEnqueue(() => trabajo()))
            Registro.Anotar("la cola del despachador rechazo el trabajo");
    }

    async void Ocultar_Click(object remitente, RoutedEventArgs args)
    {
        var ventana = App.Ventana;
        if (ventana is null) return;

        Registro.Anotar("ventana oculta 10 s");
        ventana.AppWindow.Hide();

        // Con la ventana oculta el atajo tiene que seguir contando: es
        // el escenario real, porque pastepad vive en la bandeja.
        await Task.Delay(TimeSpan.FromSeconds(10));

        ventana.AppWindow.Show();
        Registro.Anotar("ventana visible otra vez");
    }

    void AbrirLog_Click(object remitente, RoutedEventArgs args)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.
                ProcessStartInfo(Registro.Ruta) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            Registro.Fallo("abrir el log", e);
            Estado.Text = "No se pudo abrir el log: " + e.Message;
        }
    }

    void Reiniciar_Click(object remitente, RoutedEventArgs args)
    {
        _atajos = 0;
        _copias = 0;
        ContadorAtajo.Text = "0";
        ContadorPortapapeles.Text = "0";
        Ultimo.Text = string.Empty;
        Registro.Anotar("contadores a cero");
    }
}
