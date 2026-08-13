using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;
using Windows.System;
using WinRT.Interop;

namespace Pastepad.App;

/// <summary>
/// El panel. Sin marco, siempre encima y fuera de Alt+Tab, como el
/// Win+V al que sustituye.
///
/// No se cierra nunca: se esconde. Cerrarlo mataria el proceso y con el
/// el atajo global.
/// </summary>
public sealed partial class Panel : Window
{
    const string Reciente = "reciente";
    const string Guardados = "guardados";

    readonly ObservableCollection<ItemLista> _items = [];

    /// <summary>Solo las tarjetas, sin cabeceras: es lo pegable.</summary>
    readonly List<Fila> _filas = [];

    /// <summary>
    /// Lo marcado en seleccion multiple. Por referencia y no por
    /// contenido: dos entradas con el mismo texto son dos entradas.
    /// </summary>
    readonly HashSet<Elemento> _marcados =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Que grupos de Guardados estan abiertos. Se siembra del almacen al
    /// arrancar y se guarda al plegar: el usuario pidio verlos "en una
    /// lista desplegable, no extendida", y una preferencia que se pierde
    /// al cerrar el panel no es una preferencia.
    /// </summary>
    readonly Dictionary<string, bool> _grupos = [];

    readonly nint _hwnd;
    readonly OverlappedPresenter _presentador;

    string _pestana = Reciente;

    /// <summary>null es "todas las carpetas".</summary>
    string? _carpeta;

    bool _marcando;
    bool _compacta;

    public bool EstaVisible { get; private set; }

    public Panel()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);

        _presentador = OverlappedPresenter.Create();
        // Con borde y sin barra de titulo. Medido: con (false, false) la
        // ventana conservaba una banda no-cliente de 9 px arriba que
        // pintaba el sistema —la franja clara que se veia cruzando el
        // panel— y el area cliente quedaba 9 px mas corta que el marco.
        // Con (true, false) el contenido llega de borde a borde y las
        // esquinas redondeadas y la sombra las pone Windows.
        _presentador.SetBorderAndTitleBar(true, false);
        _presentador.IsAlwaysOnTop = true;
        _presentador.IsResizable = true;
        _presentador.IsMinimizable = false;
        _presentador.IsMaximizable = false;

        AppWindow.SetPresenter(_presentador);

        PonerTopes();

        // Sin esto WinUI reserva arriba la banda de la barra de titulo y
        // la pinta el sistema: se veia una franja clara cruzando el
        // panel de lado a lado. Con el contenido extendido, la ventana
        // es una sola superficie de borde a borde.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(ZonaArrastre);

        // El redondeo se pide una sola vez y solo aqui: el panel no
        // dibuja esquinas propias. Dos redondeos distintos dejaban a la
        // vista la franja entre uno y otro.
        EsquinasVentana.Redondear(_hwnd);

        // Un gestor de portapapeles no es una aplicacion que se visite:
        // se invoca. No pinta nada en Alt+Tab ni en la barra de tareas.
        AppWindow.IsShownInSwitchers = false;

        AppWindow.Hide();

        Lista.ItemsSource = _items;

        AplicarEstilo();
        PintarPestanas();
        PintarPie();

        // Lo que pintamos desde codigo —todo lo que lleva acento— no lo
        // reevalua nadie al cambiar Windows de claro a oscuro. Los
        // pinceles de la paleta si: van con ThemeResource.
        Marco.ActualThemeChanged += AlCambiarElTema;

        // Perder el foco lo cierra, como hace el propio Win+V.
        Activated += AlActivarse;

        AppWindow.Changed += AlCambiarLaVentana;
    }

    void AlCambiarElTema(FrameworkElement remitente, object args)
    {
        Estilo.Sincronizar(Marco.ActualTheme);
        Repintar();
    }

    Almacen Almacen => App.Actual.Almacen;

    /// <summary>El modo de carpetas: "menu" o "fichas".</summary>
    string ModoCarpetas => Almacen.Pref("carpetas", "menu") ?? "menu";

    // ---------------------------------------------------------- estilo

    /// <summary>
    /// Relee acento y fondo de las preferencias.
    ///
    /// Solo decide que pedirle a WinUI: con "auto" pide
    /// <see cref="ElementTheme.Default"/>, que es seguir a Windows, y a
    /// partir de ahi el cambio de tema en caliente lo hace el framework
    /// con los ThemeDictionaries. Aqui no se sondea el registro ni se
    /// fuerza un tema.
    /// </summary>
    public void AplicarEstilo()
    {
        Estilo.Aplicar(
            Almacen.Pref("acento", Estilo.AcentoDef),
            Almacen.Pref("tema", Estilo.TemaDef));

        Marco.RequestedTheme = Estilo.TemaPedido;

        // Mica es el material que usa el propio Win+V, y es media razon
        // de estar en WinUI 3: taparlo con un color plano seria pagar el
        // coste sin cobrar el beneficio. Solo se tapa cuando el usuario
        // eligio uno de los diez fondos propios, que es pedir un color
        // concreto a proposito.
        // Con un fondo propio Mica estorba: se veria el material del
        // sistema por detras de un color que el usuario eligio a mano.
        SystemBackdrop = Estilo.UsaMica
            ? new Microsoft.UI.Xaml.Media.MicaBackdrop()
            : null;

        Estilo.Sincronizar(Marco.ActualTheme);
        Repintar();
    }

    /// <summary>
    /// Repinta lo que se pinta desde codigo. Los pinceles de la paleta
    /// van con ThemeResource y se actualizan solos; el acento y todo lo
    /// que se construye a mano —pestanas, carpetas, pie, filas— no.
    /// </summary>
    void Repintar()
    {
        // Transparente y no null: null no responde al raton y el panel
        // dejaria de recibir clics en los huecos.
        Marco.Background = Estilo.UsaMica
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent)
            : Estilo.Pincel(Estilo.Actual.Fondo);

        string atajo = Almacen.Pref("atajo", Config.AtajoDef) ?? Config.AtajoDef;

        EtiquetaAtajo.Text = Config.Atajos.TryGetValue(atajo, out var legible)
            ? legible
            : atajo;

        Traducir();

        PintarPausa();
        PintarPestanas();
        PintarCarpetas();
        PintarPie();

        // Las filas leen la paleta al construir sus pinceles, asi que
        // hay que decirles que vuelvan a mirarla.
        foreach (var f in _filas) f.Refrescar();
    }

    /// <summary>
    /// Los textos que el XAML declara sueltos. Van por codigo porque el
    /// idioma se elige en caliente y un literal en el marcado no se
    /// vuelve a leer.
    /// </summary>
    void Traducir()
    {
        Buscador.PlaceholderText = Textos.T("Buscar en todo");
        TabReciente.Content = Textos.T("Reciente");
        TabGuardados.Content = Textos.T("Guardados");
        AvisoPausa.Text = "● " + Textos.T("En pausa");

        Rotular(BotonApariencia, Textos.T("Apariencia"));
        Rotular(BotonCerrar, Textos.T("Cerrar"));
    }

    /// <summary>
    /// El rotulo de un boton que solo lleva icono: el globo de ayuda y el
    /// nombre que lee un lector de pantalla, sacados del mismo texto.
    ///
    /// Van juntos a proposito. Con un glifo como unico contenido, el
    /// arbol de accesibilidad daba nombre vacio y esos botones no se
    /// podian identificar de oido; y puestos por separado, el dia que uno
    /// cambie el otro se queda atras.
    /// </summary>
    static void Rotular(UIElement control, string texto)
    {
        ToolTipService.SetToolTip(control, texto);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(control, texto);
    }

    void PintarPausa()
    {
        bool pausado = Almacen.Pref("pausado", false);

        AvisoPausa.Visibility = pausado ? Visibility.Visible : Visibility.Collapsed;

        BotonPausa.Content = pausado
            ? Estilo.Iconos.Reanudar
            : Estilo.Iconos.Pausa;

        BotonPausa.Foreground = pausado
            ? Estilo.Pincel(Estilo.Rojo)
            : Estilo.Pincel(Estilo.Actual.Medio);

        BotonPausa.BorderBrush = Estilo.Pincel(Estilo.Rojo);
        BotonPausa.BorderThickness = new Thickness(pausado ? 1 : 0);

        Rotular(
            BotonPausa,
            Textos.T(pausado ? "Reanudar la captura" : "Pausar la captura"));
    }

    // -------------------------------------------------------- ventana

    void AlActivarse(object remitente, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated
            && EstaVisible)
        {
            // Con un dialogo abierto la ventana pierde la activacion sin
            // que el usuario se haya ido a ningun sitio. Esconderla ahi
            // dejaba el dialogo huerfano en mitad de la pantalla.
            if (HayDialogo()) return;

            Esconder();
        }
    }

    bool HayDialogo() =>
        Marco.XamlRoot is { } raiz
        && Microsoft.UI.Xaml.Media.VisualTreeHelper
            .GetOpenPopupsForXamlRoot(raiz).Count > 0;

    /// <summary>
    /// Guarda el tamaño que el usuario deje. Va en logicas, no en
    /// fisicas: si no, al cambiar de monitor el panel crecia o menguaba.
    /// </summary>
    void AlCambiarLaVentana(AppWindow ventana, AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || !EstaVisible) return;

        double escala = Escala();

        // Se guarda lo que se ve, no el rectangulo de ventana: guardar
        // este ultimo hacia que el panel creciera unos pixeles en cada
        // apertura, porque al reabrirlo se le volvia a sumar el marco.
        var holgura = MarcoVentana.Holgura(_hwnd);

        int ancho = (int)Math.Round(
            (ventana.Size.Width - holgura.Ancho) / escala);
        int alto = (int)Math.Round(
            (ventana.Size.Height - holgura.Alto) / escala);

        if (ancho < Config.MinAncho || alto < Config.MinAlto) return;

        // La fila pasa a una linea sola por debajo de 340 de ancho. Se
        // mira el ancho real y no el preajuste: desde que el borde se
        // arrastra, el preajuste ya no dice la verdad.
        if (_compacta != ancho < Estilo.AnchoCompacto)
        {
            _compacta = ancho < Estilo.AnchoCompacto;
            Refrescar();
            PintarPie();
        }

        if (Almacen.Pref("ancho", 0) == ancho && Almacen.Pref("alto", 0) == alto)
            return;

        Almacen.PonerPref("ancho", ancho);
        Almacen.PonerPref("alto", alto);
    }

    double Escala()
    {
        double escala = Nativo.GetDpiForWindow(_hwnd) / 96.0;
        return escala > 0 ? escala : 1;
    }

    /// <summary>
    /// Los topes del arrastre. Se declaran sobre lo que se ve, que es de
    /// lo que habla la especificacion, pero el presentador los aplica al
    /// rectangulo de ventana — y ese lleva dentro el marco invisible de
    /// arrastre. Sin sumarselo, un maximo de 720 dejaba el panel en 706
    /// de ancho visible, y el minimo de 300 le permitia bajar a 286.
    ///
    /// Se vuelve a poner en cada apertura porque la holgura no se puede
    /// medir hasta que la ventana existe, y puede cambiar al pasar a un
    /// monitor con otra escala.
    /// </summary>
    void PonerTopes()
    {
        var holgura = MarcoVentana.Holgura(_hwnd);

        _presentador.PreferredMinimumWidth = Config.MinAncho + holgura.Ancho;
        _presentador.PreferredMinimumHeight = Config.MinAlto + holgura.Alto;
        _presentador.PreferredMaximumWidth = Config.MaxAncho + holgura.Ancho;
        _presentador.PreferredMaximumHeight = Config.MaxAlto + holgura.Alto;
    }

    /// <summary>Saca el panel junto al puntero.</summary>
    public void Asomar()
    {
        PonerTopes();

        var (ancho, alto) = TamanoGuardado();

        // AppWindow trabaja en pixeles fisicos; las medidas de config son
        // logicas. Sin esta conversion el panel sale pequeño en pantallas
        // escaladas, que es justo lo que WinUI evita en el resto.
        double escala = Escala();

        int anchoFisico = (int)Math.Round(ancho * escala);
        int altoFisico = (int)Math.Round(alto * escala);

        var (x, y) = Pantalla.JuntoAlPuntero(anchoFisico, altoFisico);

        // Al rectangulo de ventana hay que sumarle el marco invisible de
        // arrastre, que se cuenta pero no se ve. Sin esto un panel
        // pedido de 380x560 se veia de 366x553.
        var holgura = MarcoVentana.Holgura(_hwnd);

        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            x, y, anchoFisico + holgura.Ancho, altoFisico + holgura.Alto));

        _compacta = ancho < Estilo.AnchoCompacto;

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

        // El modo seleccion no sobrevive al cierre: volver y encontrarse
        // las casillas puestas de la vez anterior desconcierta.
        if (_marcando)
        {
            _marcando = false;
            _marcados.Clear();
        }
    }

    /// <summary>
    /// Lo que el usuario dejo al arrastrar los bordes. Por debajo del
    /// minimo se ignora: en el primer arranque no hay nada guardado, y un
    /// config.json editado a mano puede traer cualquier cosa.
    /// </summary>
    (int Ancho, int Alto) TamanoGuardado()
    {
        int ancho = Almacen.Pref("ancho", 0);
        int alto = Almacen.Pref("alto", 0);

        return ancho >= Config.MinAncho && alto >= Config.MinAlto
            ? (ancho, alto)
            : (Config.AnchoDef, Config.AltoDef);
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

    // ---------------------------------------------------------- lista

    /// <summary>Rehace la lista con lo que haya en el almacen.</summary>
    public void Refrescar()
    {
        string consulta = (Buscador?.Text ?? "").Trim();

        _items.Clear();
        _filas.Clear();

        string vacio;
        string iconoVacio;

        if (consulta.Length > 0)
        {
            foreach (var r in App.Actual.Indice.Buscar(consulta))
                _filas.Add(new Fila(r.Dato, _compacta));

            vacio = Textos.T("Nada coincide con esa búsqueda");
            iconoVacio = Estilo.Iconos.Buscar;
        }
        else if (_pestana == Guardados)
        {
            foreach (var s in Almacen.Snippets)
            {
                if (_carpeta is null || s.Categoria == _carpeta)
                    _filas.Add(new Fila(s, _compacta));
            }

            vacio = Textos.T("Vacío. Usa Nuevo para guardar un texto");
            iconoVacio = Estilo.Iconos.CarpetaAbierta;
        }
        else
        {
            foreach (var e in Almacen.HistOrdenado())
                _filas.Add(new Fila(e, _compacta));

            vacio = Textos.T("Copia algo y aparecerá aquí");
            iconoVacio = Estilo.Iconos.Portapapeles;
        }

        foreach (var f in _filas)
        {
            f.Marcando = _marcando;
            f.Marcada = _marcados.Contains(f.Dato);
        }

        if (_filas.Count == 0)
        {
            TextoVacio.Text = vacio;
            IconoVacio.Glyph = iconoVacio;
            Vacio.Visibility = Visibility.Visible;
        }
        else
        {
            Vacio.Visibility = Visibility.Collapsed;

            if (_pestana == Guardados && consulta.Length == 0)
                Agrupar();
            else
                foreach (var f in _filas) _items.Add(f);
        }

        PintarPestanas();
        PintarCarpetas();
        PintarPie();

        // La primera fila queda elegida, como en la maqueta 01. Sin
        // esto, Enter no tendria sobre que actuar y la barra blanca de
        // foco no se veria hasta tocar el raton.
        if (!_marcando) Lista.SelectedIndex = PrimeraFila();
    }

    /// <summary>
    /// Guardados en tres grupos plegables: marcadores, plantillas y
    /// notas.
    ///
    /// Ninguno es otro tipo de dato: son guardados que se distinguen por
    /// lo que llevan dentro —un enlace, unos [[campos]], o ni una cosa
    /// ni otra—. Se separan porque no se usan igual: uno se abre en el
    /// navegador, otro pregunta antes de pegar y el tercero se pega tal
    /// cual. La carpeta la elige el usuario; el grupo sale del texto y
    /// no hay que mantenerlo.
    ///
    /// Un enlace con [[campos]] cuenta como marcador: lo que manda es
    /// que ese clic abre el navegador.
    /// </summary>
    void Agrupar()
    {
        var marcadores = _filas.Where(f => f.EsEnlace).ToList();
        var plantillas = _filas.Where(f => f.EsPlantilla).ToList();
        var notas = _filas.Where(f => !f.EsEnlace && !f.EsPlantilla).ToList();

        // Con un solo grupo la cabecera sobra: no hay nada que separar,
        // y plegarlo dejaria la pestana en blanco.
        int cuantos = (marcadores.Count > 0 ? 1 : 0)
                    + (plantillas.Count > 0 ? 1 : 0)
                    + (notas.Count > 0 ? 1 : 0);

        bool conCabecera = cuantos > 1;

        Volcar("marcadores", Textos.T("Marcadores"), marcadores,
               Estilo.Iconos.Enlace, true, conCabecera);

        Volcar("plantillas", Textos.T("Plantillas"), plantillas,
               Estilo.Iconos.Plantilla, true, conCabecera);

        Volcar("notas", Textos.T("Notas"), notas,
               Estilo.Iconos.Nota, false, conCabecera);
    }

    /// <summary>
    /// Si un grupo esta abierto. De fabrica cerrado: con las tres
    /// cabeceras a la vista se ve de un vistazo que hay de cada cosa y
    /// se abre lo que se busca, que es como lo pidio el usuario.
    /// </summary>
    bool GrupoAbierto(string clave)
    {
        if (_grupos.TryGetValue(clave, out bool abierto)) return abierto;

        abierto = Almacen.Pref("grupo." + clave, false);
        _grupos[clave] = abierto;

        return abierto;
    }

    void Volcar(string clave, string etiqueta, List<Fila> grupo,
                string icono, bool enAcento, bool conCabecera)
    {
        if (grupo.Count == 0) return;

        bool abierto = GrupoAbierto(clave);

        if (conCabecera)
        {
            _items.Add(new Grupo(clave, etiqueta, grupo.Count, abierto,
                                 icono, enAcento));

            if (!abierto) return;
        }

        foreach (var f in grupo) _items.Add(f);
    }

    // ------------------------------------------------------- pestanas

    void PintarPestanas()
    {
        // Buscando no se resalta ninguna: la busqueda cruza las dos, y
        // dejar una encendida hacia creer que solo miraba ahi.
        bool buscando = (Buscador?.Text ?? "").Trim().Length > 0;

        Pintar(TabReciente, _pestana == Reciente && !buscando);
        Pintar(TabGuardados, _pestana == Guardados && !buscando);
    }

    static void Pintar(Button pestana, bool activa)
    {
        Vestir(
            pestana,
            activa ? Estilo.ColorAcento.Color : Estilo.Actual.Tarjeta,
            activa ? Estilo.ColorAcento.Sobre : Estilo.Actual.Medio);

        pestana.FontWeight = activa
            ? Microsoft.UI.Text.FontWeights.SemiBold
            : Microsoft.UI.Text.FontWeights.Normal;
    }

    /// <summary>
    /// Pinta un boton con un color nuestro, estados incluidos.
    ///
    /// Poner solo Background no basta: la plantilla de Button cambia ese
    /// pincel al pasar el raton y al pulsar, y una pestana de acento se
    /// volvia gris del sistema mientras el puntero estaba encima. Los
    /// pinceles de estado viven en el diccionario del propio boton, que
    /// es donde la plantilla los busca antes de subir al de la
    /// aplicacion.
    /// </summary>
    static void Vestir(Button boton, string fondo, string letra)
    {
        var relleno = Estilo.Desde(fondo);
        var tinta = Estilo.Desde(letra);

        // Sobre el color propio, el hover y el pulsado se hacen con la
        // misma tinta a poca opacidad: es como los hace WinUI, pero sin
        // perder nuestro color.
        boton.Resources["ButtonBackground"] = new SolidColorBrush(relleno);
        boton.Resources["ButtonBackgroundPointerOver"] =
            Mezcla(relleno, tinta, 0.10);
        boton.Resources["ButtonBackgroundPressed"] =
            Mezcla(relleno, tinta, 0.18);

        boton.Resources["ButtonForeground"] = new SolidColorBrush(tinta);
        boton.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(tinta);
        boton.Resources["ButtonForegroundPressed"] = new SolidColorBrush(tinta);

        boton.Resources["ButtonBorderBrush"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        boton.Resources["ButtonBorderBrushPointerOver"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        boton.Resources["ButtonBorderBrushPressed"] =
            new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        boton.Background = new SolidColorBrush(relleno);
        boton.Foreground = new SolidColorBrush(tinta);
    }

    static SolidColorBrush Mezcla(
        Windows.UI.Color uno, Windows.UI.Color otro, double parte)
    {
        byte Mitad(byte a, byte b) =>
            (byte)Math.Round(a + ((b - a) * parte));

        return new SolidColorBrush(Windows.UI.Color.FromArgb(
            255,
            Mitad(uno.R, otro.R),
            Mitad(uno.G, otro.G),
            Mitad(uno.B, otro.B)));
    }

    void Tab_Reciente(object remitente, RoutedEventArgs args) => Cambiar(Reciente);

    void Tab_Guardados(object remitente, RoutedEventArgs args) => Cambiar(Guardados);

    void Cambiar(string cual)
    {
        _pestana = cual;

        if (_marcando)
        {
            _marcando = false;
            _marcados.Clear();
        }

        Refrescar();
    }

    // ------------------------------------------------------- carpetas

    void PintarCarpetas()
    {
        if (_pestana != Guardados)
        {
            BarraCarpetas.Visibility = Visibility.Collapsed;
            return;
        }

        BarraCarpetas.Visibility = Visibility.Visible;

        bool fichas = ModoCarpetas == "fichas";

        BotonCarpeta.Visibility = fichas ? Visibility.Collapsed : Visibility.Visible;
        FichasCarpetas.Visibility = fichas ? Visibility.Visible : Visibility.Collapsed;

        if (fichas) PintarFichas();
        else PintarBotonCarpeta();
    }

    void PintarBotonCarpeta()
    {
        bool puesta = _carpeta is not null;

        NombreCarpeta.Text = _carpeta ?? Textos.T("Todas las carpetas");

        Vestir(
            BotonCarpeta,
            puesta ? Estilo.ColorAcento.Color : Estilo.Actual.Tarjeta,
            puesta ? Estilo.ColorAcento.Sobre : Estilo.Actual.Medio);

        var letra = puesta
            ? Estilo.Pincel(Estilo.ColorAcento.Sobre)
            : Estilo.Pincel(Estilo.Actual.Medio);

        NombreCarpeta.Foreground = letra;
        ChevronCarpeta.Foreground = letra;

        // La carpeta va siempre en ambar cuando no esta encendida: es la
        // senia por la que se reconoce de un vistazo. Sobre el acento no
        // se leeria, asi que ahi toma el color del texto de encima.
        IconoCarpeta.Foreground = puesta ? letra : Estilo.Pincel(Estilo.Ambar);
        IconoCarpeta.Glyph = puesta
            ? Estilo.Iconos.Carpeta
            : Estilo.Iconos.CarpetaAbierta;

        ConstruirMenuCarpetas();
    }

    void ConstruirMenuCarpetas()
    {
        MenuCarpetas.Items.Clear();

        var todas = new MenuFlyoutItem { Text = Textos.T("Todas las carpetas") };
        todas.Click += (_, _) => ElegirCarpeta(null);
        MenuCarpetas.Items.Add(todas);

        if (Almacen.Carpetas.Count > 0)
            MenuCarpetas.Items.Add(new MenuFlyoutSeparator());

        foreach (var nombre in Almacen.Carpetas)
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = nombre,
                IsChecked = nombre == _carpeta,
            };

            string cual = nombre;
            item.Click += (_, _) => ElegirCarpeta(cual);

            MenuCarpetas.Items.Add(item);
        }

        MenuCarpetas.Items.Add(new MenuFlyoutSeparator());

        var nueva = new MenuFlyoutItem { Text = Textos.T("Nueva carpeta...") };
        nueva.Click += async (_, _) => await NuevaCarpeta();
        MenuCarpetas.Items.Add(nueva);

        // Editar no depende de que haya una carpeta puesta: con "Todas
        // las carpetas" —que es lo que hay al abrir— no salia ninguna
        // forma de renombrar ni de borrar, y el usuario lo conto como
        // que las carpetas no tienen boton de editar.
        if (Almacen.Carpetas.Count > 0)
        {
            var editar = new MenuFlyoutItem { Text = Textos.T("Editar carpetas...") };
            editar.Click += async (_, _) => await EditarCarpetas();
            MenuCarpetas.Items.Add(editar);
        }

        if (_carpeta is null) return;

        var contenido = new MenuFlyoutItem
        {
            Text = Textos.T("Editar el contenido de %s...", _carpeta),
        };
        contenido.Click += async (_, _) => await EditarContenido();
        MenuCarpetas.Items.Add(contenido);

        var renombrar = new MenuFlyoutItem { Text = Textos.T("Renombrar %s", _carpeta) };
        renombrar.Click += async (_, _) => await RenombrarCarpeta();
        MenuCarpetas.Items.Add(renombrar);

        var borrar = new MenuFlyoutItem
        {
            Text = Textos.T("Eliminar %s y su contenido", _carpeta),
            Foreground = Estilo.Pincel(Estilo.Rojo),
        };
        borrar.Click += async (_, _) => await BorrarCarpeta();
        MenuCarpetas.Items.Add(borrar);
    }

    /// <summary>
    /// El otro modo: una capsula por carpeta en fila horizontal. Con
    /// muchas carpetas no caben en el ancho del panel, y por eso el
    /// desplegable es el modo de fabrica.
    /// </summary>
    void PintarFichas()
    {
        ListaFichas.Children.Clear();

        ListaFichas.Children.Add(Ficha(Textos.T("Todos"), null));

        foreach (var nombre in Almacen.Carpetas)
            ListaFichas.Children.Add(Ficha(nombre, nombre));

        var mas = new Button
        {
            Content = Estilo.Iconos.CarpetaNueva,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Height = Estilo.AltoPestana,
            MinHeight = Estilo.AltoPestana,
            MinWidth = 0,
            Width = 34,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Estilo.RCapsula),
            BorderThickness = new Thickness(0),
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            Foreground = Estilo.Pincel(Estilo.Ambar),
        };

        Rotular(mas, Textos.T("Nueva carpeta"));
        mas.Click += async (_, _) => await NuevaCarpeta();

        ListaFichas.Children.Add(mas);

        if (Almacen.Carpetas.Count == 0) return;

        // El lapiz al lado del mas: en este modo el clic derecho de la
        // ficha es lo unico que habia para renombrar, y un menu que solo
        // aparece con el boton derecho no lo encuentra nadie.
        var editar = new Button
        {
            Content = Estilo.Iconos.Editar,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 14,
            Height = Estilo.AltoPestana,
            MinHeight = Estilo.AltoPestana,
            MinWidth = 0,
            Width = 34,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(Estilo.RCapsula),
            BorderThickness = new Thickness(0),
            Background = Estilo.Pincel(Estilo.Actual.Tarjeta),
            Foreground = Estilo.Pincel(Estilo.Actual.Medio),
        };

        Rotular(editar, Textos.T("Editar carpetas"));
        editar.Click += async (_, _) => await EditarCarpetas();

        ListaFichas.Children.Add(editar);
    }

    Button Ficha(string etiqueta, string? carpeta)
    {
        bool activa = _carpeta == carpeta;

        var ficha = new Button
        {
            Content = etiqueta,
            Height = Estilo.AltoPestana,
            MinHeight = Estilo.AltoPestana,
            MinWidth = 0,
            Padding = new Thickness(Estilo.E4, 0, Estilo.E4, 0),
            CornerRadius = new CornerRadius(Estilo.RCapsula),
            BorderThickness = new Thickness(0),
            FontSize = Estilo.TMenor,
            FontWeight = activa
                ? Microsoft.UI.Text.FontWeights.SemiBold
                : Microsoft.UI.Text.FontWeights.Normal,
        };

        Vestir(
            ficha,
            activa ? Estilo.ColorAcento.Color : Estilo.Actual.Tarjeta,
            activa ? Estilo.ColorAcento.Sobre : Estilo.Actual.Medio);

        ficha.Click += (_, _) => ElegirCarpeta(carpeta);

        // Renombrar y borrar viven en el clic derecho de la ficha: en
        // este modo no hay desplegable donde ponerlos.
        if (carpeta is not null)
        {
            var menu = new MenuFlyout();

            var renombrar = new MenuFlyoutItem { Text = Textos.T("Renombrar %s", carpeta) };
            renombrar.Click += async (_, _) =>
            {
                ElegirCarpeta(carpeta);
                await RenombrarCarpeta();
            };

            var borrar = new MenuFlyoutItem
            {
                Text = Textos.T("Eliminar %s y su contenido", carpeta),
                Foreground = Estilo.Pincel(Estilo.Rojo),
            };
            borrar.Click += async (_, _) =>
            {
                ElegirCarpeta(carpeta);
                await BorrarCarpeta();
            };

            menu.Items.Add(renombrar);
            menu.Items.Add(borrar);

            ficha.ContextFlyout = menu;
        }

        return ficha;
    }

    void ElegirCarpeta(string? nombre)
    {
        _carpeta = nombre;
        Refrescar();
    }

    async Task NuevaCarpeta()
    {
        string? nombre = await Dialogos.UnaLinea(
            Marco.XamlRoot, Textos.T("Nueva carpeta"), Textos.T("Nombre de la carpeta"));

        if (nombre is null) return;

        if (!Almacen.CrearCarpeta(nombre))
        {
            Avisar(Textos.T("Ya hay una carpeta llamada %s.", nombre));
            return;
        }

        _carpeta = nombre;
        _pestana = Guardados;

        App.Actual.RefrescarLista();
    }

    /// <summary>
    /// Renombrar y quitar carpetas, todas de una vez y sin depender de
    /// cual este puesta.
    /// </summary>
    async Task EditarCarpetas()
    {
        var antes = Almacen.Carpetas
            .Select(c => (c, Almacen.ContenidoDe(c).Count))
            .ToList();

        var cambios = await Dialogos.Carpetas(Marco.XamlRoot, antes);
        if (cambios is null || cambios.Count == 0) return;

        foreach (var cambio in cambios)
        {
            if (cambio.Quitada)
            {
                Almacen.BorrarCarpeta(cambio.Nombre);
                if (_carpeta == cambio.Nombre) _carpeta = null;
                continue;
            }

            if (!Almacen.RenombrarCarpeta(cambio.Nombre, cambio.Nuevo))
            {
                Avisar(Textos.T(
                    "No se pudo renombrar: ya hay una carpeta %s.", cambio.Nuevo));
                continue;
            }

            if (_carpeta == cambio.Nombre) _carpeta = cambio.Nuevo;
        }

        App.Actual.RefrescarLista();
    }

    /// <summary>
    /// Lo de dentro de la carpeta puesta: se elige un texto y se abre en
    /// el mismo editor de siempre. En dos pasos y no en uno porque dos
    /// ContentDialog no pueden estar abiertos a la vez.
    /// </summary>
    async Task EditarContenido()
    {
        if (_carpeta is not { } carpeta) return;

        var textos = Almacen.ContenidoDe(carpeta);

        var elegido = await Dialogos.Contenido(Marco.XamlRoot, carpeta, textos);
        if (elegido is null) return;

        var nuevo = await Dialogos.Texto(
            Marco.XamlRoot, Textos.T("Editar texto"), Almacen.Carpetas,
            elegido.Categoria, elegido);

        if (nuevo is null) return;

        Almacen.ReemplazarSnippet(elegido, nuevo);
        App.Actual.RefrescarLista();
    }

    async Task RenombrarCarpeta()
    {
        if (_carpeta is not { } viejo) return;

        string? nuevo = await Dialogos.UnaLinea(
            Marco.XamlRoot, Textos.T("Renombrar carpeta"), Textos.T("Nuevo nombre"), viejo);

        if (nuevo is null || nuevo == viejo) return;

        if (!Almacen.RenombrarCarpeta(viejo, nuevo))
        {
            Avisar(Textos.T("No se pudo renombrar: ya hay una carpeta %s.", nuevo));
            return;
        }

        _carpeta = nuevo;
        App.Actual.RefrescarLista();
    }

    async Task BorrarCarpeta()
    {
        if (_carpeta is not { } nombre) return;

        int cuantos = Almacen.ContenidoDe(nombre).Count;

        string aviso = cuantos switch
        {
            0 => Textos.T("¿Eliminar la carpeta %s?", nombre),

            1 => Textos.T(
                "¿Eliminar la carpeta %s y su texto? Esto no se puede deshacer.",
                nombre),

            _ => Textos.T(
                "¿Eliminar la carpeta %s y sus %d textos? "
                + "Esto no se puede deshacer.",
                nombre, cuantos),
        };

        if (!await Dialogos.Confirmar(Marco.XamlRoot, aviso)) return;

        Almacen.BorrarCarpeta(nombre);
        _carpeta = null;

        App.Actual.RefrescarLista();
    }

    // ------------------------------------------------------------ pie

    void PintarPie()
    {
        Pie.Children.Clear();
        Pie.ColumnDefinitions.Clear();

        Pie.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Pie.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
        });
        Pie.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var izquierda = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Estilo.E2,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Grid.SetColumn(izquierda, 0);
        Pie.Children.Add(izquierda);

        if (_marcando)
        {
            izquierda.Children.Add(BotonPie(Textos.T("Todos"), "normal", MarcarTodos));
            izquierda.Children.Add(BotonPie(
                Textos.T("Borrar (%d)", _marcados.Count), "peligro",
                async () => await BorrarMarcados()));

            var cancelar = BotonPie(Textos.T("Cancelar"), "normal", AlternarMarcado);
            Grid.SetColumn(cancelar, 2);
            Pie.Children.Add(cancelar);
            return;
        }

        if (_pestana == Guardados)
        {
            izquierda.Children.Add(IconoPie(
                Estilo.Iconos.CarpetaNueva, Textos.T("Nueva carpeta"),
                async () => await NuevaCarpeta(), Estilo.Ambar));

            izquierda.Children.Add(IconoPie(
                Estilo.Iconos.Lista, Textos.T("Agregar una lista"),
                async () => await AgregarLista()));
        }
        else
        {
            izquierda.Children.Add(IconoPie(
                Estilo.Iconos.Escoba, Textos.T("Vaciar el historial"),
                async () => await Vaciar()));
        }

        // Con la ventana estrecha no caben icono y dos botones con
        // texto: el ultimo se salia por la derecha. Al apretar,
        // "Seleccionar" desaparece y "Nuevo" se queda con el signo mas.
        if (!_compacta)
            izquierda.Children.Add(BotonPie(Textos.T("Seleccionar"), "normal", AlternarMarcado));

        var nuevo = new Button
        {
            Style = (Style)Application.Current.Resources["PpBotonAcento"],
        };

        Vestir(nuevo, Estilo.ColorAcento.Color, Estilo.ColorAcento.Sobre);

        var dentro = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };

        dentro.Children.Add(new TextBlock
        {
            Text = Estilo.Iconos.Mas,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });

        if (!_compacta)
        {
            dentro.Children.Add(new TextBlock
            {
                Text = Textos.T("Nuevo"),
                FontSize = Estilo.TMenor,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        nuevo.Content = dentro;

        // Tambien lleva rotulo aunque suela verse la palabra: en panel
        // estrecho el texto desaparece y queda solo el signo mas.
        Rotular(nuevo, Textos.T("Nuevo"));

        nuevo.Click += async (_, _) => await Nuevo();

        Grid.SetColumn(nuevo, 2);
        Pie.Children.Add(nuevo);
    }

    Button BotonPie(string texto, string estilo, Action alPulsar)
    {
        var b = new Button
        {
            Content = texto,
            Style = (Style)Application.Current.Resources["PpBoton"],
        };

        if (estilo == "peligro")
        {
            Vestir(b, Estilo.Peligro, "#FFFFFF");
        }
        else
        {
            Vestir(b, Estilo.Actual.Tarjeta, Estilo.Actual.Medio);
            b.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        }

        b.Click += (_, _) => alPulsar();
        return b;
    }

    Button IconoPie(string glifo, string tip, Action alPulsar, string? color = null)
    {
        var b = new Button
        {
            Content = glifo,
            Style = (Style)Application.Current.Resources["PpIconoPie"],
            Foreground = Estilo.Pincel(color ?? Estilo.Actual.Medio),
        };

        Rotular(b, tip);
        b.Click += (_, _) => alPulsar();
        return b;
    }

    // -------------------------------------------- seleccion multiple

    void AlternarMarcado()
    {
        _marcando = !_marcando;
        _marcados.Clear();
        Refrescar();
    }

    void MarcarTodos()
    {
        if (_marcados.Count == _filas.Count) _marcados.Clear();
        else foreach (var f in _filas) _marcados.Add(f.Dato);

        foreach (var f in _filas) f.Marcada = _marcados.Contains(f.Dato);

        PintarPie();
    }

    async Task BorrarMarcados()
    {
        if (_marcados.Count == 0) return;

        string aviso = Textos.T(
            _marcados.Count == 1
                ? "¿Borrar %d elemento? Esto no se puede deshacer."
                : "¿Borrar %d elementos? Esto no se puede deshacer.",
            _marcados.Count);

        if (!await Dialogos.Confirmar(Marco.XamlRoot, aviso)) return;

        Almacen.BorrarVarios([.. _marcados]);

        _marcados.Clear();
        _marcando = false;

        App.Actual.RefrescarLista();
    }

    // ------------------------------------------------------- acciones

    async Task Nuevo()
    {
        var snippet = await Dialogos.Texto(
            Marco.XamlRoot,
            Textos.T("Nuevo texto"),
            Almacen.Carpetas,
            _carpeta ?? Almacen.Carpetas.FirstOrDefault() ?? Config.CarpetaDef);

        if (snippet is null) return;

        Almacen.AnadirSnippet(snippet);

        _pestana = Guardados;
        App.Actual.RefrescarLista();
    }

    async Task AgregarLista()
    {
        string carpeta = _carpeta
            ?? Almacen.Carpetas.FirstOrDefault()
            ?? Config.CarpetaDef;

        var lineas = await Dialogos.Lista(Marco.XamlRoot, carpeta);
        if (lineas is null) return;

        foreach (var linea in lineas)
        {
            Almacen.AnadirSnippet(Modelo.CrearSnippet(linea, carpeta));
        }

        _pestana = Guardados;
        _carpeta = carpeta;

        App.Actual.RefrescarLista();
    }

    async Task Vaciar()
    {
        if (!await Dialogos.Confirmar(
                Marco.XamlRoot,
                Textos.T("¿Vaciar el historial? Los fijados se quedan.")))
        {
            return;
        }

        Almacen.VaciarHistorial();
        App.Actual.RefrescarLista();
    }

    async Task Editar(Fila fila)
    {
        if (fila.Dato is Snippet viejo)
        {
            var nuevo = await Dialogos.Texto(
                Marco.XamlRoot, Textos.T("Editar texto"), Almacen.Carpetas,
                viejo.Categoria, viejo);

            if (nuevo is null) return;

            Almacen.ReemplazarSnippet(viejo, nuevo);
            App.Actual.RefrescarLista();
            return;
        }

        // Una entrada del historial se edita guardandola: es lo que
        // hacia la version anterior con "Editar y guardar...".
        var guardado = await Dialogos.Texto(
            Marco.XamlRoot,
            Textos.T("Editar y guardar..."),
            Almacen.Carpetas,
            _carpeta ?? Almacen.Carpetas.FirstOrDefault() ?? Config.CarpetaDef,
            new Snippet { Runs = [Modelo.CrearFragmento(fila.Texto)] });

        if (guardado is null) return;

        Almacen.AnadirSnippet(guardado);

        _pestana = Guardados;
        App.Actual.RefrescarLista();
    }

    /// <summary>
    /// Pega la fila. Si es una plantilla con [[campos]], primero
    /// pregunta; si es un enlace, abre el navegador en vez de pegar.
    /// </summary>
    async Task Usar(Fila fila, bool sinFormato = false)
    {
        if (fila.EsEnlace)
        {
            App.Actual.AbrirEnlace(fila.Texto);
            return;
        }

        if (fila.Dato is Entrada entrada)
        {
            App.Actual.Pegar(entrada);
            return;
        }

        if (fila.Dato is not Snippet snippet) return;

        var campos = Modelo.CamposDe(fila.Texto);

        var runs = snippet.Runs;

        if (campos.Count > 0)
        {
            var valores = await Dialogos.Campos(Marco.XamlRoot, campos);
            if (valores is null) return;

            runs = Modelo.Rellenar(snippet.Runs, valores);
        }

        App.Actual.PegarFragmentos(runs, sinFormato);
    }

    // ------------------------------------------------------- teclado

    void Escape_Invoked(
        KeyboardAccelerator remitente, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        if (_marcando)
        {
            AlternarMarcado();
            return;
        }

        Esconder();
    }

    void Buscador_TextChanged(object remitente, TextChangedEventArgs args) =>
        Refrescar();

    /// <summary>
    /// El buscador enmarcado en acento cuando tiene el foco, como la
    /// maqueta 12. La caja es un Border aparte porque el TextBox va sin
    /// borde propio.
    /// </summary>
    void Buscador_Foco(object remitente, RoutedEventArgs args)
    {
        bool tiene = Buscador.FocusState != FocusState.Unfocused;

        // Sin foco el borde toma el color del relleno y no transparente:
        // medido, un borde transparente de 1 px dejaba el buscador en 40
        // px pintados donde la maqueta pide 42.
        MarcoBuscador.BorderBrush = tiene
            ? Estilo.Pincel(Estilo.ColorAcento.Color)
            : Estilo.Pincel(Estilo.Actual.Elevado);
    }

    /// <summary>
    /// Escribir y pulsar Enter usa el primer resultado: es el camino
    /// rapido, y evita tener que soltar el teclado para el raton.
    /// </summary>
    async void Buscador_KeyDown(object remitente, KeyRoutedEventArgs args)
    {
        switch (args.Key)
        {
            case VirtualKey.Enter when _filas.Count > 0:
                args.Handled = true;
                await Usar(_filas[0]);
                break;

            case VirtualKey.Down when _items.Count > 0:
                args.Handled = true;
                Lista.SelectedIndex = PrimeraFila();
                Lista.Focus(FocusState.Programmatic);
                break;
        }
    }

    int PrimeraFila()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] is Fila) return i;
        }

        return -1;
    }

    async void Lista_KeyDown(object remitente, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter && Lista.SelectedItem is Fila fila)
        {
            args.Handled = true;
            await Usar(fila);
        }
    }

    // --------------------------------------------------------- raton

    void Lista_SelectionChanged(object remitente, SelectionChangedEventArgs args)
    {
        foreach (var quitada in args.RemovedItems)
        {
            if (quitada is Fila f) f.Activa = false;
        }

        // Una cabecera de grupo no se selecciona: no hay nada que pegar.
        if (Lista.SelectedItem is Grupo)
        {
            Lista.SelectedItem = null;
            return;
        }

        if (Lista.SelectedItem is Fila fila) fila.Activa = true;
    }

    async void Lista_ItemClick(object remitente, ItemClickEventArgs args)
    {
        switch (args.ClickedItem)
        {
            // El unico sitio donde se pliega un grupo. La cabecera no
            // engancha ademas su propio Tapped: con los dos, el segundo
            // llegaba cuando el Refrescar del primero ya habia reciclado
            // el contenedor, leia el DataContext del grupo siguiente y
            // plegaba ese. Medido: un clic en "Marcadores" cerraba los
            // dos grupos y uno en "Notas" no hacia nada.
            case Grupo grupo:
                bool abierto = !GrupoAbierto(grupo.Clave);

                _grupos[grupo.Clave] = abierto;
                Almacen.PonerPref("grupo." + grupo.Clave, abierto);

                Refrescar();
                break;

            case Fila fila when _marcando:
                if (!_marcados.Remove(fila.Dato)) _marcados.Add(fila.Dato);
                fila.Marcada = _marcados.Contains(fila.Dato);
                PintarPie();
                break;

            case Fila fila:
                await Usar(fila);
                break;
        }
    }

    void Fila_Entra(object remitente, PointerRoutedEventArgs args)
    {
        if (remitente is FrameworkElement { DataContext: Fila fila })
            fila.Encima = true;
    }

    void Fila_Sale(object remitente, PointerRoutedEventArgs args)
    {
        if (remitente is FrameworkElement { DataContext: Fila fila })
            fila.Encima = false;
    }

    // ------------------------------------------- menu de tres puntos

    static Fila? DeMenu(object remitente) =>
        (remitente as FrameworkElement)?.DataContext as Fila;

    void Menu_Abrir(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is { } fila) App.Actual.AbrirEnlace(fila.Texto);
    }

    async void Menu_Pegar(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is { } fila) await Usar(fila);
    }

    async void Menu_PegarPlano(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is { } fila) await Usar(fila, sinFormato: true);
    }

    void Menu_Copiar(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is { } fila) App.Actual.CopiarSolo(fila.Dato);
    }

    void Menu_Fijar(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is not { Dato: Entrada entrada }) return;

        // Fijar es algo que el usuario hace a proposito, asi que se
        // escribe al disco en el acto y no con el volcado diferido.
        Almacen.Fijar(entrada);
        App.Actual.RefrescarLista();
    }

    async void Menu_Editar(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is { } fila) await Editar(fila);
    }

    void Menu_Borrar(object remitente, RoutedEventArgs args)
    {
        if (DeMenu(remitente) is not { } fila) return;

        Almacen.Borrar(fila.Dato);
        App.Actual.RefrescarLista();
    }

    // ------------------------------------------------------ cabecera

    void Pausa_Click(object remitente, RoutedEventArgs args)
    {
        Almacen.PonerPref("pausado", !Almacen.Pref("pausado", false));
        PintarPausa();
    }

    void Cerrar_Click(object remitente, RoutedEventArgs args) => Esconder();

    async void Apariencia_Click(object remitente, RoutedEventArgs args)
    {
        var antes = new Dialogos.Preferencias(
            Almacen.Pref("acento", Estilo.AcentoDef) ?? Estilo.AcentoDef,
            Almacen.Pref("tema", Estilo.TemaDef) ?? Estilo.TemaDef,
            Almacen.Pref("atajo", Config.AtajoDef) ?? Config.AtajoDef,
            ModoCarpetas,
            Almacen.Pref("idioma", Textos.IdiomaDef) ?? Textos.IdiomaDef);

        var elegido = await Dialogos.Apariencia(Marco.XamlRoot, antes);
        if (elegido is null) return;

        Almacen.PonerPref("acento", elegido.Acento);
        Almacen.PonerPref("tema", elegido.Tema);
        Almacen.PonerPref("carpetas", elegido.Carpetas);
        Almacen.PonerPref("idioma", elegido.Idioma);

        Textos.Idioma = elegido.Idioma;

        if (elegido.Atajo != antes.Atajo)
        {
            Almacen.PonerPref("atajo", elegido.Atajo);

            if (!App.Actual.PonerAtajo(elegido.Atajo))
            {
                Avisar(Textos.T(
                    "%s ya lo usa otro programa. Se dejó el anterior.",
                    Config.Atajos.GetValueOrDefault(elegido.Atajo, elegido.Atajo)));

                Almacen.PonerPref("atajo", antes.Atajo);
                App.Actual.PonerAtajo(antes.Atajo);
            }
        }

        AplicarEstilo();
        Refrescar();
    }
}
