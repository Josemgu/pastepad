using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;
using Windows.System;
using WinRT.Interop;

namespace Pastepad.App;

/// <summary>Una fila de la lista, ya lista para dibujar.</summary>
public sealed class Fila(Entrada entrada)
{
    public Entrada Entrada { get; } = entrada;

    /// <summary>
    /// Glifos de Segoe Fluent Icons, la fuente de iconos de Windows 11.
    /// Van por codigo y no por el caracter suelto para que se vea cual
    /// es cada uno al leer.
    /// </summary>
    public string Icono =>
        Entrada.Pin ? ""          // chincheta
        : Entrada.EsImagen ? ""   // imagen
        : "";                     // texto

    public string Resumen => Entrada.EsImagen
        ? Path.GetFileName(Entrada.Ruta ?? "imagen")
        : Modelo.UnaLinea(Entrada.Texto ?? "", 90);
}

/// <summary>
/// El panel. Sin marco, siempre encima y fuera de Alt+Tab, como el
/// Win+V al que sustituye.
///
/// No se cierra nunca: se esconde. Cerrarlo mataria el proceso y con el
/// el atajo global.
/// </summary>
public sealed partial class Panel : Window
{
    readonly ObservableCollection<Fila> _filas = [];

    readonly nint _hwnd;
    readonly OverlappedPresenter _presentador;

    public bool EstaVisible { get; private set; }

    public Panel()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);

        _presentador = OverlappedPresenter.Create();
        _presentador.SetBorderAndTitleBar(false, false);
        _presentador.IsAlwaysOnTop = true;
        _presentador.IsResizable = true;
        _presentador.IsMinimizable = false;
        _presentador.IsMaximizable = false;
        _presentador.PreferredMinimumWidth = Config.MinAncho;
        _presentador.PreferredMinimumHeight = Config.MinAlto;
        _presentador.PreferredMaximumWidth = Config.MaxAncho;
        _presentador.PreferredMaximumHeight = Config.MaxAlto;

        AppWindow.SetPresenter(_presentador);

        // Un gestor de portapapeles no es una aplicacion que se visite:
        // se invoca. No pinta nada en Alt+Tab ni en la barra de tareas.
        AppWindow.IsShownInSwitchers = false;

        AppWindow.Hide();

        Lista.ItemsSource = _filas;

        // Perder el foco lo cierra, como hace el propio Win+V.
        Activated += AlActivarse;

        AppWindow.Changed += AlCambiarLaVentana;
    }

    void AlActivarse(object remitente, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated
            && EstaVisible)
        {
            Esconder();
        }
    }

    /// <summary>
    /// Guarda el tamaño que el usuario deje. Va en logicas, no en
    /// fisicas: si no, al cambiar de monitor el panel crecia o menguaba.
    /// </summary>
    void AlCambiarLaVentana(AppWindow ventana, AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || !EstaVisible) return;

        double escala = Escala();

        int ancho = (int)Math.Round(ventana.Size.Width / escala);
        int alto = (int)Math.Round(ventana.Size.Height / escala);

        if (ancho < Config.MinAncho || alto < Config.MinAlto) return;

        var almacen = App.Actual.Almacen;

        if (almacen.Pref("ancho", 0) == ancho && almacen.Pref("alto", 0) == alto)
            return;

        almacen.PonerPref("ancho", ancho);
        almacen.PonerPref("alto", alto);
    }

    double Escala()
    {
        double escala = Nativo.GetDpiForWindow(_hwnd) / 96.0;
        return escala > 0 ? escala : 1;
    }

    /// <summary>Saca el panel junto al puntero.</summary>
    public void Asomar()
    {
        var (ancho, alto) = TamanoGuardado();

        // AppWindow trabaja en pixeles fisicos; las medidas de config son
        // logicas. Sin esta conversion el panel sale pequeño en pantallas
        // escaladas, que es justo lo que WinUI evita en el resto.
        double escala = Escala();

        int anchoFisico = (int)Math.Round(ancho * escala);
        int altoFisico = (int)Math.Round(alto * escala);

        var (x, y) = Pantalla.JuntoAlPuntero(anchoFisico, altoFisico);

        AppWindow.MoveAndResize(
            new Windows.Graphics.RectInt32(x, y, anchoFisico, altoFisico));

        Buscador.Text = "";
        Refrescar();

        AppWindow.Show();
        EstaVisible = true;

        Activate();

        // Activate() no basta: Windows no deja que un proceso en segundo
        // plano se ponga delante, y el panel salia visible pero sin
        // foco, con el buscador sin recibir lo que se escribiera. Se usa
        // el mismo enganche de hilos que para devolver el foco al pegar.
        if (!Foco.TraerAlFrente(_hwnd))
            Registro.Anotar("el panel no consiguio el primer plano");

        Buscador.Focus(FocusState.Programmatic);
    }

    public void Esconder()
    {
        if (!EstaVisible) return;

        EstaVisible = false;
        AppWindow.Hide();
        Aviso.Visibility = Visibility.Collapsed;
    }

    static (int Ancho, int Alto) TamanoGuardado()
    {
        var almacen = App.Actual.Almacen;

        int ancho = almacen.Pref("ancho", 0);
        int alto = almacen.Pref("alto", 0);

        if (ancho >= Config.MinAncho && alto >= Config.MinAlto)
            return (ancho, alto);

        return Config.Tamanos[Config.TamanoDef];
    }

    /// <summary>Rehace la lista con lo que haya en el almacen.</summary>
    public void Refrescar()
    {
        string consulta = Buscador?.Text ?? "";

        _filas.Clear();

        if (consulta.Trim().Length > 0)
        {
            foreach (var r in App.Actual.Indice.Buscar(consulta))
            {
                if (r.Dato is Entrada e) _filas.Add(new Fila(e));
            }
        }
        else
        {
            foreach (var e in App.Actual.Almacen.HistOrdenado())
                _filas.Add(new Fila(e));
        }
    }

    /// <summary>
    /// Un problema que el usuario tiene que ver. Nada de fallos mudos:
    /// si el atajo no se pudo registrar, se dice cual es y por que.
    /// </summary>
    public void Avisar(string texto)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Aviso.Text = texto;
            Aviso.Visibility = Visibility.Visible;
        });
    }

    // ------------------------------------------------------- el teclado

    void Escape_Invoked(
        KeyboardAccelerator remitente, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Esconder();
    }

    void Buscador_TextChanged(object remitente, TextChangedEventArgs args) =>
        Refrescar();

    /// <summary>
    /// Escribir y pulsar Enter pega el primer resultado: es el camino
    /// rapido, y evita tener que soltar el teclado para el raton.
    /// </summary>
    void Buscador_KeyDown(object remitente, KeyRoutedEventArgs args)
    {
        switch (args.Key)
        {
            case VirtualKey.Enter when _filas.Count > 0:
                args.Handled = true;
                App.Actual.Pegar(_filas[0].Entrada);
                break;

            case VirtualKey.Down when _filas.Count > 0:
                args.Handled = true;
                Lista.SelectedIndex = 0;
                Lista.Focus(FocusState.Programmatic);
                break;
        }
    }

    void Lista_KeyDown(object remitente, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter && Lista.SelectedItem is Fila fila)
        {
            args.Handled = true;
            App.Actual.Pegar(fila.Entrada);
        }
    }

    // -------------------------------------------------------- el raton

    void Lista_ItemClick(object remitente, ItemClickEventArgs args)
    {
        if (args.ClickedItem is Fila fila) App.Actual.Pegar(fila.Entrada);
    }

    /// <summary>
    /// El menu del clic derecho actua sobre la fila pinchada, no sobre
    /// la que estuviera seleccionada de antes.
    /// </summary>
    void Fila_RightTapped(object remitente, RightTappedRoutedEventArgs args)
    {
        if (remitente is FrameworkElement { DataContext: Fila fila })
            Lista.SelectedItem = fila;
    }

    static Fila? DeMenu(object remitente) =>
        (remitente as FrameworkElement)?.DataContext as Fila;

    void Fijar_Click(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is not { } fila) return;

        // Fijar es algo que el usuario hace a proposito, asi que se
        // escribe al disco en el acto y no con el volcado diferido.
        App.Actual.Almacen.Fijar(fila.Entrada);
        App.Actual.RefrescarLista();
    }

    void Borrar_Click(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is not { } fila) return;

        App.Actual.Almacen.Borrar(fila.Entrada);
        App.Actual.RefrescarLista();
    }
}
