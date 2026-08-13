using Microsoft.UI.Xaml;
using Pastepad.App.Sistema;
using Pastepad.Nucleo;

namespace Pastepad.App;

/// <summary>
/// Coordina todo: el almacen, el buzon de Windows, la bandeja y el
/// panel. Es el unico sitio donde esas cuatro cosas se conocen.
/// </summary>
public partial class App : Application
{
    internal static App Actual => (App)Current;

    internal Almacen Almacen { get; private set; } = null!;
    internal Indice Indice { get; private set; } = null!;

    Buzon? _buzon;
    Bandeja? _bandeja;
    Panel? _panel;
    Timer? _volcado;

    /// <summary>
    /// La ventana que tenia el foco cuando se pulso el atajo. Se guarda
    /// en ese instante y no despues: en cuanto el panel aparece, esta
    /// informacion ya se perdio.
    /// </summary>
    nint _ventanaPrevia;

    /// <summary>
    /// La ultima secuencia del portapapeles que escribimos nosotros. Sin
    /// esto, pegar volveria a meter en el historial lo que se acaba de
    /// pegar.
    /// </summary>
    uint _secuenciaPropia;

    uint _ultimaSecuencia;

    public App()
    {
        InitializeComponent();

        UnhandledException += (_, a) =>
        {
            Registro.Fallo("Application.UnhandledException", a.Exception);

            // No se marca como manejada: un fallo en el arbol de XAML que
            // no sabemos tratar no debe dejarse pasar en silencio.
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Almacen = new Almacen(
            Program.CarpetaDatos is { } carpeta ? Rutas.EnCarpeta(carpeta) : null,
            Registro.Fallo,
            Registro.Anotar);
        Indice = new Indice(Almacen);

        // Antes de crear el panel: sus textos se resuelven al construirlo.
        Textos.Idioma = Almacen.Pref("idioma", Textos.IdiomaDef)
                        ?? Textos.IdiomaDef;

        _panel = new Panel();

        // Si los datos no se pudieron leer, el usuario tiene que
        // enterarse ahora y no cuando descubra que falta medio
        // historial. Se abre el panel a proposito: un aviso que nadie
        // mira no es un aviso.
        if (Almacen.Problema is not null)
        {
            Registro.Anotar("ARRANQUE DEGRADADO: " + Almacen.Problema);
            _panel.Avisar(Almacen.Problema);
            _panel.Asomar();
        }

        // El buzon se crea en el hilo de interfaz, que es donde
        // RegisterHotKey acepta el HWND y donde la bomba de WinUI
        // despacha sus mensajes.
        _buzon = new Buzon();
        _buzon.Atajo += AlAtajo;
        _buzon.Portapapeles += AlPortapapeles;
        _buzon.Mostrarse += () => MostrarPanel();
        _buzon.Salir += Cerrar;

        string atajo = Almacen.Pref("atajo", Config.AtajoDef) ?? Config.AtajoDef;

        if (!_buzon.PonerAtajo(atajo) && _buzon.PonerAtajo(Config.AtajoDef))
            Registro.Anotar($"'{atajo}' no se pudo poner; se uso el de fabrica");

        _bandeja = new Bandeja(_buzon.Handle, "pastepad");

        _ultimaSecuencia = Portapapeles.Secuencia();

        // El historial se vuelca cada pocos segundos en vez de en cada
        // copia: escribir el JSON entero costaba 7,8 ms y mas de un mega
        // por cada Ctrl+C.
        _volcado = new Timer(
            _ => _panel?.DispatcherQueue.TryEnqueue(() => Almacen.Volcar()),
            null, Almacen.IntervaloVolcado, Almacen.IntervaloVolcado);

        if (_buzon.Problema is not null)
            _panel.Avisar(_buzon.Problema);
    }

    // ------------------------------------------------------- el atajo

    void AlAtajo()
    {
        // Lo primero de todo, antes de tocar ninguna ventana.
        _ventanaPrevia = Foco.VentanaActiva();

        if (_panel is null) return;

        if (_panel.EstaVisible) _panel.Esconder();
        else MostrarPanel();
    }

    void MostrarPanel()
    {
        _panel?.Asomar();
    }

    // -------------------------------------------------- el portapapeles

    void AlPortapapeles()
    {
        uint secuencia = Portapapeles.Secuencia();

        // WM_CLIPBOARDUPDATE no llega una vez por copia sino una por
        // cada sesion que abre quien copia: PowerShell dispara tres.
        // El contador de Windows es lo que distingue una copia de
        // verdad de un aviso repetido.
        if (secuencia == _ultimaSecuencia) return;
        _ultimaSecuencia = secuencia;

        // Lo que acabamos de poner nosotros para pegarlo no se reanota.
        if (secuencia == _secuenciaPropia) return;

        if (Almacen.Pref("pausado", false)) return;

        var contenido = Portapapeles.Leer();

        switch (contenido.Tipo)
        {
            case TipoContenido.Privado:
                Registro.Anotar("copia marcada como privada: no se guarda");
                break;

            case TipoContenido.Texto when contenido.Texto is not null:
                if (Almacen.Anotar(new Entrada
                {
                    Tipo = Entrada.Texto_,
                    Texto = contenido.Texto,
                }))
                {
                    RefrescarLista();
                }
                break;

            case TipoContenido.Imagen when contenido.Imagen is not null:
                if (Almacen.GuardarImagen(Portapapeles.DibABmp(contenido.Imagen)))
                    RefrescarLista();
                break;
        }
    }

    /// <summary>
    /// El indice cachea el texto normalizado de cada entrada, asi que
    /// hay que avisarle cuando el almacen cambia.
    /// </summary>
    internal void RefrescarLista()
    {
        Indice.Invalidar();
        _panel?.Refrescar();
    }

    // ------------------------------------------------------- el pegado

    /// <summary>
    /// Copia la entrada, devuelve el foco a donde estaba y pega alli.
    /// </summary>
    internal async void Pegar(Entrada entrada)
    {
        try
        {
            bool copiado = entrada.EsImagen
                ? entrada.Ruta is not null && Portapapeles.CopiarImagen(entrada.Ruta)
                : Portapapeles.Copiar(
                    [Modelo.CrearFragmento(entrada.Texto ?? "")], sinFormato: true);

            await TrasCopiar(copiado);
        }
        catch (Exception e)
        {
            Registro.Fallo("Pegar", e);
        }
    }

    /// <summary>
    /// Pega un texto con formato: es lo que distingue un guardado de una
    /// entrada del historial, que siempre va plana.
    /// </summary>
    internal async void PegarFragmentos(
        IReadOnlyList<Fragmento> fragmentos, bool sinFormato)
    {
        try
        {
            await TrasCopiar(Portapapeles.Copiar(fragmentos, sinFormato));
        }
        catch (Exception e)
        {
            Registro.Fallo("PegarFragmentos", e);
        }
    }

    /// <summary>
    /// Devuelve el foco a donde estaba y pega alli. Es la parte
    /// delicada del ciclo y por eso vive en un solo sitio: guardar el
    /// hwnd anterior, engancharle el hilo y soltar Ctrl+V.
    /// </summary>
    async Task TrasCopiar(bool copiado)
    {
        if (!copiado)
        {
            _panel?.Avisar(Textos.T("No se pudo copiar al portapapeles."));
            return;
        }

        _secuenciaPropia = Portapapeles.Secuencia();
        _ultimaSecuencia = _secuenciaPropia;

        _panel?.Esconder();

        if (!Foco.Devolver(_ventanaPrevia))
        {
            // Queda copiado: el usuario puede pegar a mano. Es peor
            // no decirselo que decirlo.
            _panel?.Avisar(Textos.T("Copiado, pero no pude volver a la ventana "
                                  + "anterior. Pega con Ctrl+V."));
            return;
        }

        // Windows necesita un instante para asentar el primer plano
        // antes de aceptar las teclas.
        await Task.Delay(60);

        Foco.PegarConTeclado();
    }

    /// <summary>Deja el contenido en el portapapeles sin pegarlo.</summary>
    internal void CopiarSolo(Elemento elemento)
    {
        try
        {
            bool copiado = elemento switch
            {
                Entrada { EsImagen: true, Ruta: { } ruta } =>
                    Portapapeles.CopiarImagen(ruta),

                Entrada entrada => Portapapeles.Copiar(
                    [Modelo.CrearFragmento(entrada.Texto ?? "")], sinFormato: true),

                Snippet snippet => Portapapeles.Copiar(snippet.Runs),

                _ => false,
            };

            if (!copiado)
            {
                _panel?.Avisar(Textos.T("No se pudo copiar al portapapeles."));
                return;
            }

            _secuenciaPropia = Portapapeles.Secuencia();
            _ultimaSecuencia = _secuenciaPropia;

            _panel?.Esconder();
        }
        catch (Exception e)
        {
            Registro.Fallo("CopiarSolo", e);
        }
    }

    /// <summary>
    /// Un enlace se abre, no se pega. Solo cuando el texto entero es la
    /// direccion: un parrafo que la menciona de pasada no cuenta.
    /// </summary>
    internal void AbrirEnlace(string texto)
    {
        _panel?.Esconder();

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Modelo.UrlDe(texto),
                UseShellExecute = true,
            });
        }
        catch (Exception e)
        {
            Registro.Fallo("AbrirEnlace", e);
            _panel?.Avisar(Textos.T("No se pudo abrir el enlace."));
        }
    }

    /// <summary>
    /// Cambia el atajo global en caliente. False si Windows no lo da
    /// —normalmente porque otro programa se lo quedo—, y entonces la
    /// interfaz tiene que decirlo en vez de callarse.
    /// </summary>
    internal bool PonerAtajo(string atajo) => _buzon?.PonerAtajo(atajo) ?? false;

    // -------------------------------------------------------- el cierre

    void Cerrar()
    {
        Registro.Anotar("cierre pedido desde la bandeja");

        Almacen.Volcar(forzar: true);

        _volcado?.Dispose();
        _bandeja?.Dispose();
        _buzon?.Dispose();

        Exit();
    }
}
